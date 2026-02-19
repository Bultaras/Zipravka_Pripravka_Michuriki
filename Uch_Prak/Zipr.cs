using System;
using System.Collections.Generic;

namespace GasStation
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "АЗС 'ЗАПРАВКА'";
            Console.WriteLine("=== АЗС 'ЗАПРАВКА' ===\n");
            new StationMenu().ShowMainMenu();
            Console.WriteLine("\nСпасибо, что выбрали нашу АЗС!");
            Console.ReadKey();
        }
    }

    public class StationMenu
    {
        private StationManager manager = new StationManager();

        public StationMenu()
        {
            // ИЗМЕНЕНИЕ: Добавлены новые виды топлива (АИ-100 и Газ) для расширения ассортимента
            var f1 = new Fuel(1, "АИ-92", 52.50m, "Бензин");
            var f2 = new Fuel(2, "АИ-95", 55.20m, "Бензин");
            var f3 = new Fuel(3, "Дизель", 53.80m, "Дизель");
            var f4 = new Fuel(4, "АИ-100", 58.90m, "Бензин");
            var f5 = new Fuel(5, "Газ", 28.50m, "Газ");

            // ИЗМЕНЕНИЕ: Цикл для добавления всех видов топлива в менеджер
            foreach (var f in new[] { f1, f2, f3, f4, f5 }) manager.AddFuel(f);

            // ИЗМЕНЕНИЕ: Добавлены новые колонки с разной максимальной емкостью
            var p1 = new FuelPump { Id = 1, FuelType = f1, MaxCapacity = 1000 };
            var p2 = new FuelPump { Id = 2, FuelType = f2, MaxCapacity = 1000 };
            var p3 = new FuelPump { Id = 3, FuelType = f3, MaxCapacity = 1000 };
            var p4 = new FuelPump { Id = 4, FuelType = f4, MaxCapacity = 800 };
            var p5 = new FuelPump { Id = 5, FuelType = f5, MaxCapacity = 600 };

            // ИЗМЕНЕНИЕ: Заполнение колонок топливом (разный процент для реалистичности)
            // Ранее колонки были пустыми (TODO в InitializeStationData)
            p1.Refill(850); p2.Refill(920); p3.Refill(780); p4.Refill(650); p5.Refill(500);
            foreach (var p in new[] { p1, p2, p3, p4, p5 }) manager.AddFuelPump(p);

            // ИЗМЕНЕНИЕ: Добавлены тестовые клиенты для демонстрации работы программы
            manager.RegisterCustomer("Иван Петров", "А123БВ", 1500);
            manager.RegisterCustomer("Петр Иванов", "В456ГД", 3000);
            manager.RegisterCustomer("Сергей Сидоров", "Е789ЖЗ", 500);
        }

        // ИЗМЕНЕНИЕ: Реализация метода ShowAvailableFuels (TODO 1)
        // Ранее метод был пустым, только выводил заголовок
        public void ShowAvailableFuels()
        {
            Console.WriteLine("\n--- ДОСТУПНОЕ ТОПЛИВО ---");
            Console.WriteLine("ID  Название    Цена     Тип");
            // Получаем список топлива через manager.GetAllFuels() и выводим каждый элемент
            foreach (var f in manager.GetAllFuels())
                Console.WriteLine($"{f.Id,-3} {f.Name,-11} {f.PricePerLiter,6:F2} {f.FuelType}");
        }

        // ИЗМЕНЕНИЕ: Полная реализация процесса заправки (TODO 2)
        // Ранее метод был пустым, содержал только комментарии с 13 шагами
        public void ProcessRefueling()
        {
            Console.WriteLine("\n--- ЗАПРАВКА ---");
            // 1. Запрос номера автомобиля
            Console.Write("Номер авто: ");
            string num = Console.ReadLine().Trim().ToUpper();
            if (string.IsNullOrEmpty(num)) { Console.WriteLine("Ошибка!"); return; }

            // 2. Поиск клиента по номеру
            Customer c = manager.FindCustomerByCarNumber(num);
            // 3. Если клиент не найден - регистрация нового
            if (c == null)
            {
                Console.WriteLine("Новый клиент.");
                Console.Write("Имя: ");
                string name = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(name)) name = "Клиент";
                Console.Write("Баланс: ");
                if (!decimal.TryParse(Console.ReadLine(), out decimal b) || b < 0) b = 0;
                c = manager.RegisterCustomer(name, num, b);
                Console.WriteLine($"ID: {c.Id}");
            }
            Console.WriteLine($"{c.Name} | Баланс: {c.Balance:F2}");

            // 4. Показ доступного топлива
            ShowAvailableFuels();
            // 5. Выбор типа топлива
            Console.Write("ID топлива: ");
            if (!int.TryParse(Console.ReadLine(), out int fid)) return;
            Fuel fuel = manager.GetAllFuels().Find(f => f.Id == fid);
            if (fuel == null) return;

            // 6. Выбор свободной колонки с этим топливом
            var pumps = manager.GetAllPumps().FindAll(p => p.FuelType.Id == fid && p.CurrentFuel > 0);
            if (pumps.Count == 0) { Console.WriteLine("Нет топлива"); return; }

            Console.WriteLine("Колонки:");
            foreach (var p in pumps) Console.WriteLine($"#{p.Id} - {p.CurrentFuel:F2}л");
            Console.Write("Номер колонки: ");
            if (!int.TryParse(Console.ReadLine(), out int pid)) return;
            FuelPump pump = pumps.Find(p => p.Id == pid);
            if (pump == null) return;

            // 7. Запрос количества литров
            Console.Write("Литры (0 - отмена): ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal liters) || liters <= 0) return;

            // 8. Проверка достаточности топлива в колонке
            if (!pump.HasEnoughFuel(liters))
            {
                Console.Write($"Доступно {pump.CurrentFuel:F2}л. Заправить? (д/н): ");
                if (Console.ReadLine().Trim().ToLower() == "д") liters = pump.CurrentFuel;
                else return;
            }

            // 9. Расчет стоимости
            decimal cost = fuel.CalculateCost(liters);
            // 10. Проверка и списание оплаты
            if (!c.PayForRefueling(cost))
            {
                Console.WriteLine($"Нужно {cost:F2}, есть {c.Balance:F2}");
                return;
            }

            // 11. Выполнение заправки (уменьшение остатка в колонке)
            decimal actual = pump.Refuel(liters);
            // 12. Добавление записи в историю клиента
            c.AddToHistory(fuel, actual, cost);
            // 13. Фиксация продажи в общей выручке
            manager.RecordSale(cost);

            Console.WriteLine($"Заправлено: {actual:F2}л, Сумма: {cost:F2}руб, Баланс: {c.Balance:F2}руб");
        }

        // ИЗМЕНЕНИЕ: Реализация метода ShowStationStats (TODO 3)
        // Ранее метод был пустым, выводил только заголовок
        public void ShowStationStats()
        {
            Console.WriteLine("\n--- СТАТИСТИКА ---");
            // Вывод общей выручки через manager.GetTotalRevenue()
            Console.WriteLine($"Выручка: {manager.GetTotalRevenue():F2}руб");
            // Вывод количества зарегистрированных клиентов
            Console.WriteLine($"Клиентов: {manager.GetCustomerCount()}");
            Console.WriteLine("\nКолонки:");
            // Вывод остатков топлива на всех колонках
            foreach (var p in manager.GetAllPumps()) p.ShowPumpInfo();
        }

        // ИЗМЕНЕНИЕ: Новый метод для пополнения баланса клиента (пункт меню 4)
        // Ранее вместо этого было "Функция в разработке"
        public void TopUpBalanceMenu(Customer customer = null)
        {
            Console.WriteLine("\n--- ПОПОЛНЕНИЕ ---");
            if (customer == null)
            {
                Console.Write("Номер авто: ");
                customer = manager.FindCustomerByCarNumber(Console.ReadLine().Trim().ToUpper());
                if (customer == null) { Console.WriteLine("Не найден"); return; }
            }
            Console.Write("Сумма: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal a) && a > 0)
                customer.TopUpBalance(a);
        }

        // ИЗМЕНЕНИЕ: Новый метод для просмотра истории заправок клиента (пункт меню 5)
        public void ShowCustomerHistory()
        {
            Console.WriteLine("\n--- ИСТОРИЯ ---");
            Console.Write("Номер авто: ");
            Customer c = manager.FindCustomerByCarNumber(Console.ReadLine().Trim().ToUpper());
            if (c == null) Console.WriteLine("Не найден");
            else c.ShowHistory();
        }

        // ИЗМЕНЕНИЕ: Новый метод для пополнения топливных колонок (пункт меню 6)
        public void RefillPumpMenu()
        {
            Console.WriteLine("\n--- ПОПОЛНЕНИЕ КОЛОНКИ ---");
            foreach (var p in manager.GetAllPumps()) p.ShowPumpInfo();
            Console.Write("Номер колонки: ");
            if (!int.TryParse(Console.ReadLine(), out int id)) return;
            FuelPump p2 = manager.GetAllPumps().Find(p => p.Id == id);
            if (p2 == null) return;
            Console.Write("Литры: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal l) && l > 0)
                p2.Refill(l);
        }

        // Готовый метод - главное меню (не изменялся, только добавлены новые пункты)
        public void ShowMainMenu()
        {
            bool run = true;
            while (run)
            {
                Console.Clear();
                Console.WriteLine("=== АЗС 'ЗАПРАВКА' ===");
                Console.WriteLine("1. Цены\n2. Заправить\n3. Статистика\n4. Пополнить баланс\n5. История\n6. Пополнить колонку\n7. Выход");
                Console.Write("Выбор: ");
                switch (Console.ReadLine())
                {
                    case "1": ShowAvailableFuels(); break;
                    case "2": ProcessRefueling(); break;
                    case "3": ShowStationStats(); break;
                    case "4": TopUpBalanceMenu(null); break;
                    case "5": ShowCustomerHistory(); break;
                    case "6": RefillPumpMenu(); break;
                    case "7": run = false; break;
                    default: Console.WriteLine("Ошибка"); break;
                }
                if (run) { Console.WriteLine("\nEnter..."); Console.ReadLine(); }
            }
        }
    }

    public class StationManager
    {
        private List<Customer> customers = new List<Customer>();
        private List<FuelPump> pumps = new List<FuelPump>();
        private List<Fuel> fuels = new List<Fuel>();
        // ИЗМЕНЕНИЕ: Добавлен словарь для учета продаж по типам топлива (новый функционал)
        private Dictionary<string, (decimal Liters, decimal Revenue)> sales = new Dictionary<string, (decimal, decimal)>();
        private int nextId = 1;
        private decimal revenue = 0;

        // ИЗМЕНЕНИЕ: Реализация метода RegisterCustomer (TODO 1)
        // Ранее возвращал null, содержал только комментарии
        public Customer RegisterCustomer(string name, string car, decimal balance)
        {
            // Создание нового клиента с уникальным ID
            var c = new Customer { Id = nextId++, Name = name, CarNumber = car.ToUpper() };
            // Установка начального баланса
            c.TopUpBalance(balance);
            // Добавление клиента в список
            customers.Add(c);
            // Возврат созданного клиента
            return c;
        }

        // ИЗМЕНЕНИЕ: Реализация метода FindCustomerByCarNumber (TODO 2)
        // Ранее возвращал null, содержал только комментарии
        public Customer FindCustomerByCarNumber(string car) => 
            customers.Find(c => c.CarNumber.Equals(car, StringComparison.OrdinalIgnoreCase));

        // ИЗМЕНЕНИЕ: Реализация метода RecordSale (TODO 3)
        // Ранее был пустым, только комментарий
        public void RecordSale(decimal amount) => revenue += amount;

        // Готовые методы (не изменялись)
        public void AddFuelPump(FuelPump p) => pumps.Add(p);
        public void AddFuel(Fuel f) => fuels.Add(f);
        public List<Fuel> GetAllFuels() => fuels;
        public List<FuelPump> GetAllPumps() => pumps;
        public decimal GetTotalRevenue() => revenue;
        
        // ИЗМЕНЕНИЕ: Добавлен новый метод для получения количества клиентов
        public int GetCustomerCount() => customers.Count;
        
        // ИЗМЕНЕНИЕ: Добавлен метод для получения продаж по типам топлива
        public Dictionary<string, (decimal Liters, decimal Revenue)> GetSalesByFuelType() => sales;
        
        // ИЗМЕНЕНИЕ: Добавлен метод для получения топ-клиентов по тратам
        public List<Customer> GetTopCustomers(int count)
        {
            customers.Sort((a, b) => b.GetTotalSpent().CompareTo(a.GetTotalSpent()));
            return customers.GetRange(0, Math.Min(count, customers.Count));
        }
    }

    public class FuelPump
    {
        public int Id { get; set; }
        public Fuel FuelType { get; set; }
        
        // ИЗМЕНЕНИЕ: Добавлено свойство CurrentFuel (TODO 1)
        // Приватный сеттер защищает от прямого изменения извне
        public decimal CurrentFuel { get; private set; }
        public decimal MaxCapacity { get; set; } = 1000;
        
        // ИЗМЕНЕНИЕ: Реализация метода заправки (TODO 2)
        public decimal Refuel(decimal l)
        {
            // Если топлива достаточно - заправляем запрошенное, если нет - всё что есть
            decimal a = HasEnoughFuel(l) ? l : CurrentFuel;
            CurrentFuel -= a; // Уменьшаем остаток в колонке
            return a; // Возвращаем реально заправленное количество
        }
        
        // ИЗМЕНЕНИЕ: Реализация проверки достаточности топлива (TODO 3)
        public bool HasEnoughFuel(decimal l) => CurrentFuel >= l;
        
        // ИЗМЕНЕНИЕ: Реализация пополнения колонки (TODO 1)
        public void Refill(decimal l) => CurrentFuel = Math.Min(CurrentFuel + l, MaxCapacity);
        
        // ИЗМЕНЕНИЕ: Добавлен вывод остатка и процента заполнения в ShowPumpInfo (TODO 1)
        public void ShowPumpInfo()
        {
            double p = (double)(CurrentFuel / MaxCapacity) * 100;
            Console.WriteLine($"#{Id} {FuelType.Name}: {CurrentFuel,5:F1}/{MaxCapacity}л ({p:F0}%)");
        }
    }

    public class Fuel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal PricePerLiter { get; set; }
        
        // ИЗМЕНЕНИЕ: Добавлено свойство FuelType для хранения типа топлива (TODO 1)
        public string FuelType { get; set; }
        
        // ИЗМЕНЕНИЕ: В конструкторе добавлена проверка цены (TODO 2) и сохранение типа (TODO 1)
        public Fuel(int id, string name, decimal price, string type)
        {
            Id = id; 
            Name = name; 
            // Если цена отрицательная, устанавливаем минимальное значение 0.01
            PricePerLiter = price < 0 ? 0.01m : price; 
            FuelType = type; // Сохраняем тип топлива
        }
        
        // ИЗМЕНЕНИЕ: Реализовано информативное строковое представление (TODO 3)
        // Ранее возвращало только Name
        public override string ToString() => $"{Name} - {PricePerLiter:F2} руб/л ({FuelType})";
        
        // Готовый метод для расчета стоимости
        public decimal CalculateCost(decimal l) => PricePerLiter * l;
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CarNumber { get; set; }
        
        // ИЗМЕНЕНИЕ: Добавлено свойство Balance с приватным сеттером (TODO 1)
        // Приватный сеттер защищает баланс от прямого изменения
        public decimal Balance { get; private set; }
        
        private List<RefuelingRecord> history = new List<RefuelingRecord>();
        
        public class RefuelingRecord
        {
            public DateTime Date { get; set; }
            public Fuel FuelType { get; set; }
            public decimal Liters { get; set; }
            public decimal Cost { get; set; }
        }
        
        // ИЗМЕНЕНИЕ: Реализация пополнения баланса (TODO 1)
        public void TopUpBalance(decimal a)
        {
            if (a <= 0) return; // Проверка на положительную сумму
            Balance += a;
            Console.WriteLine($"Баланс: {Balance:F2}руб");
        }
        
        // ИЗМЕНЕНИЕ: Реализация оплаты заправки (TODO 2)
        public bool PayForRefueling(decimal cost)
        {
            if (cost <= 0 || Balance < cost) return false; // Проверка достаточности средств
            Balance -= cost; // Списание стоимости
            return true;
        }
        
        // ИЗМЕНЕНИЕ: Реализация добавления записи в историю (TODO 3)
        public void AddToHistory(Fuel f, decimal l, decimal c) =>
            history.Add(new RefuelingRecord { Date = DateTime.Now, FuelType = f, Liters = l, Cost = c });
        
        // ИЗМЕНЕНИЕ: Новый метод для подсчета общей суммы потраченных средств
        public decimal GetTotalSpent()
        {
            decimal t = 0;
            foreach (var h in history) t += h.Cost;
            return t;
        }
        
        // ИЗМЕНЕНИЕ: Улучшенный метод показа истории с итогами
        public void ShowHistory()
        {
            Console.WriteLine($"\n{Name} {CarNumber}");
            if (history.Count == 0) { Console.WriteLine("Нет заправок"); return; }
            decimal tl = 0, tc = 0;
            foreach (var h in history)
            {
                Console.WriteLine($"{h.Date:dd.MM.yy} {h.FuelType.Name,-6} {h.Liters,5:F1}л = {h.Cost,6:F2}руб");
                tl += h.Liters; tc += h.Cost;
            }
            Console.WriteLine($"Итого: {tl:F1}л, {tc:F2}руб");
        }
        
        // ИЗМЕНЕНИЕ: Добавлен вывод баланса в информацию о клиенте (TODO 1)
        public void ShowCustomerInfo() =>
            Console.WriteLine($"{Name} | {CarNumber} | Баланс: {Balance:F2}руб | Заправок: {history.Count}");
    }
}