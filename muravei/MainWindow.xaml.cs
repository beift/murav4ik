using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace muravei
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Размер одной клетки в пикселях
        private const double CellSize = 45;

        // Размер стрелки
        private const double AntSize = 30;

        // Продолжительность одной анимации
        private const int AnimationDuration = 250;

        // Игровое поле
        public ObservableCollection<Cell> Cells { get; }
            = new ObservableCollection<Cell>();

        // Списки команд
        public ObservableCollection<string> WhiteCommands { get; }
            = new ObservableCollection<string>();

        public ObservableCollection<string> BlackCommands { get; }
            = new ObservableCollection<string>();

        // Размеры игрового поля
        private int _boardRows = 10;
        private int _boardColumns = 10;

        public int BoardRows
        {
            get => _boardRows;
            private set
            {
                _boardRows = value;
                OnPropertyChanged();
            }
        }

        public int BoardColumns
        {
            get => _boardColumns;
            private set
            {
                _boardColumns = value;
                OnPropertyChanged();
            }
        }

        // Положение муравья
        private int _antRow;
        private int _antColumn;

        // Направление:
        // 0 - вверх
        // 1 - вправо
        // 2 - вниз
        // 3 - влево
        private int _antDirection;

        // Угол поворота стрелки
        private double _antAngle;

        // Исходное состояние муравья
        private int _initialRow;
        private int _initialColumn;
        private int _initialDirection;
        private double _initialAngle;

        // Исходные цвета клеток
        private bool[] _initialColors;

        // Состояние программы
        private bool _isRunning;
        private bool _isPaused;

        private CancellationTokenSource _cancellationTokenSource;

        // Сигнал для продолжения после паузы
        private TaskCompletionSource<bool> _resumeSignal;

        private readonly Random _random = new Random();

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            CreateBoardInternal(10, 10);

            PauseButton.IsEnabled = false;
        }

        // ==========================================
        // СОЗДАНИЕ ИГРОВОГО ПОЛЯ
        // ==========================================

        private void CreateBoard_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(RowsTextBox.Text, out int rows) ||
                !int.TryParse(ColumnsTextBox.Text, out int columns))
            {
                MessageBox.Show(
                    "Введите корректные размеры поля.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (rows < 1 || rows > 50 ||
                columns < 1 || columns > 50)
            {
                MessageBox.Show(
                    "Размер поля должен быть от 1 до 50.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            CancelExecution();

            CreateBoardInternal(rows, columns);
        }

        private void CreateBoardInternal(int rows, int columns)
        {
            BoardRows = rows;
            BoardColumns = columns;

            Cells.Clear();

            // Создаём клетки.
            // По умолчанию все клетки белые.

            for (int i = 0; i < rows * columns; i++)
            {
                Cells.Add(new Cell
                {
                    IsBlack = _random.Next(2) == 1
                });
            }

            // Устанавливаем размеры поля.
            // При больших размерах появится прокрутка.

            double boardWidth = columns * CellSize;
            double boardHeight = rows * CellSize;

            Board.Width = boardWidth;
            Board.Height = boardHeight;

            AntCanvas.Width = boardWidth;
            AntCanvas.Height = boardHeight;

            // Генерируем случайное положение муравья.

            _antRow = _random.Next(rows);
            _antColumn = _random.Next(columns);

            // Случайное направление.
            _antDirection = _random.Next(4);

            _antAngle = _antDirection * 90;

            // Сохраняем исходное состояние.

            SaveInitialState();

            // Обновляем стрелку.
            UpdateAntPosition();
        }

        // ==========================================
        // СОХРАНЕНИЕ ИСХОДНОГО СОСТОЯНИЯ
        // ==========================================

        private void SaveInitialState()
        {
            _initialColors = Cells
                .Select(cell => cell.IsBlack)
                .ToArray();

            _initialRow = _antRow;
            _initialColumn = _antColumn;

            _initialDirection = _antDirection;
            _initialAngle = _antAngle;
        }

        // ==========================================
        // ПОЗИЦИЯ МУРАВЬЯ
        // ==========================================

        private void UpdateAntPosition()
        {
            double x = _antColumn * CellSize
                       + (CellSize - AntSize) / 2;

            double y = _antRow * CellSize
                       + (CellSize - AntSize) / 2;

            Canvas.SetLeft(AntArrow, x);
            Canvas.SetTop(AntArrow, y);

            RotateTransform rotation =
                (RotateTransform)AntArrow.RenderTransform;

            rotation.BeginAnimation(
                RotateTransform.AngleProperty, null);

            rotation.Angle = _antAngle;
        }

        // ==========================================
        // ДОБАВЛЕНИЕ КОМАНД
        // ==========================================

        private void AddWhiteCommand_Click(
            object sender, RoutedEventArgs e)
        {
            AddCommand(WhiteCommandComboBox, WhiteCommands);
        }

        private void AddBlackCommand_Click(
            object sender, RoutedEventArgs e)
        {
            AddCommand(BlackCommandComboBox, BlackCommands);
        }

        private void AddCommand(
            ComboBox comboBox,
            ObservableCollection<string> commands)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
            {
                string command = item.Content.ToString();

                commands.Add(command);
            }
        }

        // ==========================================
        // УДАЛЕНИЕ КОМАНД
        // ==========================================

        private void RemoveWhiteCommand_Click(
            object sender, RoutedEventArgs e)
        {
            if (WhiteCommandsList.SelectedItem is string command)
            {
                WhiteCommands.Remove(command);
            }
        }

        private void RemoveBlackCommand_Click(
            object sender, RoutedEventArgs e)
        {
            if (BlackCommandsList.SelectedItem is string command)
            {
                BlackCommands.Remove(command);
            }
        }

        // ==========================================
        // ЗАПУСК ПРОГРАММЫ
        // ==========================================

        private async void Start_Click(
            object sender, RoutedEventArgs e)
        {
            if (_isRunning)
                return;

            if (WhiteCommands.Count == 0 &&
                BlackCommands.Count == 0)
            {
                MessageBox.Show(
                    "Добавьте хотя бы одну команду.",
                    "Внимание",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            _isRunning = true;

            StartButton.IsEnabled = false;
            PauseButton.IsEnabled = true;

            _cancellationTokenSource =
                new CancellationTokenSource();

            CancellationToken token =
                _cancellationTokenSource.Token;

            try
            {
                await RunProgramAsync(token);
            }
            catch (OperationCanceledException)
            {
                // Выполнение остановлено сбросом.
            }
            finally
            {
                _isRunning = false;
                _isPaused = false;

                _resumeSignal?.TrySetResult(true);
                _resumeSignal = null;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                StartButton.IsEnabled = true;
                PauseButton.IsEnabled = false;

                PauseButton.Content = "Ⅱ  Пауза";
            }
        }

        // ==========================================
        // ОСНОВНОЙ ЦИКЛ РАБОТЫ МУРАВЬЯ
        // ==========================================

        private async Task RunProgramAsync(
            CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                await WaitIfPausedAsync(token);

                // Получаем текущую клетку.
                Cell currentCell = GetCurrentCell();

                // В зависимости от цвета выбираем список команд.
                string[] commands = currentCell.IsBlack
                    ? BlackCommands.ToArray()
                    : WhiteCommands.ToArray();

                if (commands.Length == 0)
                {
                    MessageBox.Show(
                        currentCell.IsBlack
                            ? "Для чёрных клеток не заданы команды."
                            : "Для белых клеток не заданы команды.",
                        "Выполнение остановлено",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                // Выполняем команды по очереди.
                foreach (string command in commands)
                {
                    token.ThrowIfCancellationRequested();

                    await WaitIfPausedAsync(token);

                    bool continueExecution =
                        await ExecuteCommandAsync(command, token);

                    if (!continueExecution)
                        return;
                }
            }
        }

        // Получение текущей клетки муравья.
        private Cell GetCurrentCell()
        {
            int index =
                _antRow * BoardColumns + _antColumn;

            return Cells[index];
        }

        // ==========================================
        // ВЫПОЛНЕНИЕ ОДНОЙ КОМАНДЫ
        // ==========================================

        private async Task<bool> ExecuteCommandAsync(
            string command,
            CancellationToken token)
        {
            switch (command)
            {
                case "Повернуть направо":

                    await TurnAsync(true, token);
                    break;

                case "Повернуть налево":

                    await TurnAsync(false, token);
                    break;

                case "Идти вперёд":

                    return await MoveForwardAsync(token);

                case "Красить":

                    // Меняем цвет текущей клетки.
                    Cell cell = GetCurrentCell();

                    cell.IsBlack = !cell.IsBlack;

                    await Task.Delay(100, token);

                    break;
            }

            return true;
        }

        // ==========================================
        // ПОВОРОТ МУРАВЬЯ
        // ==========================================

        private async Task TurnAsync(
            bool right,
            CancellationToken token)
        {
            if (right)
            {
                _antDirection = (_antDirection + 1) % 4;
                _antAngle += 90;
            }
            else
            {
                _antDirection = (_antDirection + 3) % 4;
                _antAngle -= 90;
            }

            RotateTransform rotation =
                (RotateTransform)AntArrow.RenderTransform;

            double currentAngle = rotation.Angle;

            await AnimateValueAsync(
                rotation,
                RotateTransform.AngleProperty,
                currentAngle,
                _antAngle,
                token);
        }

        // ==========================================
        // ДВИЖЕНИЕ ВПЕРЁД
        // ==========================================

        private async Task<bool> MoveForwardAsync(
            CancellationToken token)
        {
            int newRow = _antRow;
            int newColumn = _antColumn;

            // Определяем новую позицию
            // в зависимости от направления.

            switch (_antDirection)
            {
                case 0:
                    newRow--;
                    break;

                case 1:
                    newColumn++;
                    break;

                case 2:
                    newRow++;
                    break;

                case 3:
                    newColumn--;
                    break;
            }

            // Проверяем выход за границы поля.
            // При выходе муравей останавливается.

            if (newRow < 0 || newRow >= BoardRows ||
                newColumn < 0 || newColumn >= BoardColumns)
            {
                MessageBox.Show(
                    "Муравей достиг границы поля.",
                    "Выполнение остановлено",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return false;
            }

            // Вычисляем координаты новой клетки.

            double targetX = newColumn * CellSize
                             + (CellSize - AntSize) / 2;

            double targetY = newRow * CellSize
                             + (CellSize - AntSize) / 2;

            // Получаем текущие координаты стрелки.

            double currentX = Canvas.GetLeft(AntArrow);
            double currentY = Canvas.GetTop(AntArrow);

            // Анимируем только изменяющуюся координату.

            if (newColumn != _antColumn)
            {
                await AnimateValueAsync(
                    AntArrow,
                    Canvas.LeftProperty,
                    currentX,
                    targetX,
                    token);
            }
            else
            {
                await AnimateValueAsync(
                    AntArrow,
                    Canvas.TopProperty,
                    currentY,
                    targetY,
                    token);
            }

            // Обновляем позицию после завершения анимации.

            _antRow = newRow;
            _antColumn = newColumn;

            return true;
        }

        // ==========================================
        // АНИМАЦИЯ
        // ==========================================


        private async Task AnimateValueAsync(
            DependencyObject target,
            DependencyProperty property,
            double from,
            double to,
            CancellationToken token)
        {
            DoubleAnimation animation =
                new DoubleAnimation(
                    from,
                    to,
                    TimeSpan.FromMilliseconds(AnimationDuration))
                {
                    EasingFunction = new QuadraticEase
                    {
                        EasingMode = EasingMode.EaseInOut
                    }
                };

            // Запускаем анимацию через интерфейс IAnimatable
            ((IAnimatable)target).BeginAnimation(property, animation);

            try
            {
                await Task.Delay(AnimationDuration, token);
            }
            catch
            {
                ((IAnimatable)target).BeginAnimation(property, null);
                throw;
            }

            // Удаляем анимацию и фиксируем конечное значение
            ((IAnimatable)target).BeginAnimation(property, null);

            target.SetValue(property, to);
        }


        // ==========================================
        // ПАУЗА И ПРОДОЛЖЕНИЕ
        // ==========================================

        private async void Pause_Click(
            object sender, RoutedEventArgs e)
        {
            if (!_isRunning)
                return;

            if (!_isPaused)
            {
                // Включаем паузу.
                _isPaused = true;

                _resumeSignal =
                    new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);

                PauseButton.Content = "Продолжить";
            }
            else
            {
                // Продолжаем выполнение.
                _isPaused = false;

                _resumeSignal?.TrySetResult(true);
                _resumeSignal = null;

                PauseButton.Content = "Пауза";
            }
        }

        private async Task WaitIfPausedAsync(
            CancellationToken token)
        {
            while (_isPaused)
            {
                TaskCompletionSource<bool> signal =
                    _resumeSignal;

                if (signal != null)
                {
                    await signal.Task;

                    token.ThrowIfCancellationRequested();
                }
            }
        }


        // ==========================================
        // СБРОС ПРОГРАММЫ
        // ==========================================

        private void Reset_Click(
            object sender, RoutedEventArgs e)
        {
            CancelExecution();

            // Восстанавливаем исходные цвета клеток.

            for (int i = 0; i < Cells.Count; i++)
            {
                Cells[i].IsBlack = _initialColors[i];
            }

            // Возвращаем муравья в исходную позицию.

            _antRow = _initialRow;
            _antColumn = _initialColumn;

            _antDirection = _initialDirection;
            _antAngle = _initialAngle;

            // Убираем активные анимации.

            AntArrow.BeginAnimation(Canvas.LeftProperty, null);
            AntArrow.BeginAnimation(Canvas.TopProperty, null);

            RotateTransform rotation =
                (RotateTransform)AntArrow.RenderTransform;

            rotation.BeginAnimation(
                RotateTransform.AngleProperty, null);

            // Обновляем положение стрелки.

            UpdateAntPosition();
        }

        // Отмена выполняющегося цикла.

        private void CancelExecution()
        {
            _cancellationTokenSource?.Cancel();

            _isPaused = false;

            _resumeSignal?.TrySetResult(true);
            _resumeSignal = null;

            PauseButton.Content = "Пауза";
        }

        // ==========================================
        // PROPERTY CHANGED
        // ==========================================

        protected void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
