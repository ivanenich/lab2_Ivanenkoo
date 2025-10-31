using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Lab2_Console
{
    // ===== 2 enum =====
    public enum Rarity { Common = 1, Rare = 2, Epic = 3, Legendary = 4 }
    public enum ItemKind { Weapon = 1, Armor = 2, Potion = 3, Misc = 4 }

    // ===== 1 struct =====
    public struct ItemSize
    {
        public double Weight;  // >= 0
        public double Length;  // >= 0
        public int Slots;      // >= 0

        public override string ToString()
        {
            return "[вес=" + Weight + ", длина=" + Length + ", слоты=" + Slots + "]";
        }
    }

    // ===== 1 другой класс =====
    public class Crafter
    {
        public string Name = "Unknown";
        public string City; // можно пусто

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(City) ? Name : (Name + " (" + City + ")");
        }
    }

    // ===== Главный класс для хранения =====
    public class GameItem : IComparable<GameItem>
    {
        private static int _nextId = 1; // авто-генерация id

        public int Id;                // генерируется
        public string Name = "";      // строка (обязательная)
        public string Description;    // строка (может быть пусто)
        public Rarity Rarity = Rarity.Common;   // enum #1
        public ItemKind Kind = ItemKind.Misc;   // enum #2
        public ItemSize Size;         // struct
        public Crafter Maker = new Crafter(); // другой класс
        public int RequiredLevel;     // число 1..60
        public double Price;          // число >= 0
        public int Durability;        // число 0..100
        public DateTime CreatedAt;    // генерируется

        public static GameItem CreateNew()
        {
            var it = new GameItem();
            it.Id = _nextId++;
            it.CreatedAt = DateTime.Now;
            return it;
        }

        public int CompareTo(GameItem other)
        {
            if (other == null) return 1;
            return Price.CompareTo(other.Price);
        }

        public override string ToString()
        {
            return "#" + Id + ": " + Name + " (" + Kind + ", " + Rarity + "), цена " + Price.ToString(CultureInfo.InvariantCulture)
                 + ", ур." + RequiredLevel + ", прочн." + Durability + ", создано " + CreatedAt.ToString("g")
                 + ", размер " + Size + ", создатель: " + Maker + (string.IsNullOrWhiteSpace(Description) ? "" : ", опис.: " + Description);
        }

        public static void FixNextId(IEnumerable<GameItem> all)
        {
            int max = 0;
            foreach (var x in all) if (x.Id > max) max = x.Id;
            _nextId = max + 1;
        }
    }

    // ===== Приложение =====
    public class App
    {
        // Коллекция по варианту 10 — Dictionary<int, GameItem>
        private readonly Dictionary<int, GameItem> _items = new Dictionary<int, GameItem>();

        // CSV-файл (вариант 10)
        private string _filePath = "items.csv";

        public void Run(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            // файл: из аргумента или спросим
            if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                _filePath = args[0];
            else
            {
                Console.Write("Имя файла (Enter = items.csv): ");
                var f = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(f)) _filePath = f;
            }

            LoadFromCsv();
            Console.WriteLine("Готово. Введите 'help' для списка команд.");

            while (true)
            {
                Console.Write("\n> ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                line = line.Trim();
                string cmd = line, arg = "";
                int sp = line.IndexOf(' ');
                if (sp >= 0) { cmd = line.Substring(0, sp); arg = line.Substring(sp + 1).Trim(); }
                cmd = cmd.ToLowerInvariant();

                try
                {
                    switch (cmd)
                    {
                        case "help": PrintHelp(); break;
                        case "info": PrintInfo(); break;
                        case "show": Show(); break;
                        case "insert": Insert(); break;
                        case "update": Update(arg); break;
                        case "remove_key": RemoveKey(arg); break;
                        case "clear": Clear(); break;
                        case "save": SaveToCsv(); break;
                        case "execute_script": ExecuteScript(arg); break;
                        case "exit": return;

                        // Доп. команды варианта 10
                        case "filter_kind": FilterKind(arg); break;
                        case "group_by_rarity": GroupByRarity(); break;
                        case "count": Console.WriteLine(_items.Count); break;

                        default: Console.WriteLine("Неизвестная команда. help — список команд."); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ОШИБКА] " + ex.Message);
                }
            }
        }

        // ----- команды -----
        private void PrintHelp()
        {
            Console.WriteLine("Базовые:");
            Console.WriteLine("  help                   — помощь");
            Console.WriteLine("  info                   — инфо о коллекции");
            Console.WriteLine("  show                   — показать все элементы");
            Console.WriteLine("  insert                 — добавить элемент (по полям)");
            Console.WriteLine("  update <id>            — обновить элемент по id");
            Console.WriteLine("  remove_key <id>        — удалить по id");
            Console.WriteLine("  clear                  — очистить всю коллекцию");
            Console.WriteLine("  save                   — сохранить в файл CSV");
            Console.WriteLine("  execute_script <file>  — выполнить команды из файла (без рекурсии)");
            Console.WriteLine("  exit                   — выход");
            Console.WriteLine("Доп. (вариант 10):");
            Console.WriteLine("  filter_kind <Weapon|Armor|Potion|Misc>");
            Console.WriteLine("  group_by_rarity");
            Console.WriteLine("  count");
        }

        private void PrintInfo()
        {
            Console.WriteLine("Тип коллекции: Dictionary<int, GameItem>");
            Console.WriteLine("Файл: " + _filePath);
            Console.WriteLine("Количество элементов: " + _items.Count);
            Console.WriteLine("Инициализация: " + DateTime.Now.ToString("g"));
        }

        private void Show()
        {
            if (_items.Count == 0) { Console.WriteLine("Пусто."); return; }
            foreach (var kv in _items) Console.WriteLine(kv.Value);
        }

        private void Insert()
        {
            var it = ReadItemFromConsole(false, 0);
            _items[it.Id] = it; // ключ = Id
            Console.WriteLine("Добавлено.");
        }

        private void Update(string arg)
        {
            int id;
            if (!int.TryParse(arg, out id)) { Console.WriteLine("update <id>"); return; }
            if (!_items.ContainsKey(id)) { Console.WriteLine("Нет такого id."); return; }

            var it = ReadItemFromConsole(true, id);
            _items[id] = it;
            Console.WriteLine("Обновлено.");
        }

        private void RemoveKey(string arg)
        {
            int id;
            if (!int.TryParse(arg, out id)) { Console.WriteLine("remove_key <id>"); return; }
            if (_items.Remove(id)) Console.WriteLine("Удалено.");
            else Console.WriteLine("Нет такого id.");
        }

        private void Clear()
        {
            _items.Clear();
            Console.WriteLine("Очищено.");
        }

        // ----- фильтр/группировка/счёт -----
        private void FilterKind(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
            {
                Console.WriteLine("filter_kind <Weapon|Armor|Potion|Misc>");
                return;
            }
            ItemKind kind;
            if (!Enum.TryParse<ItemKind>(arg, true, out kind))
            {
                Console.WriteLine("Неизвестный тип.");
                return;
            }
            foreach (var it in _items.Values) if (it.Kind == kind) Console.WriteLine(it);
        }

        private void GroupByRarity()
        {
            if (_items.Count == 0) { Console.WriteLine("Пусто."); return; }
            Array vals = Enum.GetValues(typeof(Rarity));
            for (int i = 0; i < vals.Length; i++)
            {
                var r = (Rarity)vals.GetValue(i);
                Console.WriteLine("\n" + r + ":");
                foreach (var it in _items.Values) if (it.Rarity == r) Console.WriteLine("  " + it);
            }
        }

        // ----- чтение/запись CSV -----
        // Формат (порядок столбцов):
        // Id;Name;Description;Rarity;Kind;Weight;Length;Slots;MakerName;MakerCity;RequiredLevel;Price;Durability;CreatedAt
        private void SaveToCsv()
        {
            try
            {
                using (var sw = new StreamWriter(_filePath, false, new UTF8Encoding(false)))
                {
                    sw.WriteLine("Id;Name;Description;Rarity;Kind;Weight;Length;Slots;MakerName;MakerCity;RequiredLevel;Price;Durability;CreatedAt");
                    foreach (var it in _items.Values)
                    {
                        sw.WriteLine(
                            it.Id + ";" +
                            EscapeCsv(it.Name) + ";" +
                            EscapeCsv(it.Description) + ";" +
                            it.Rarity + ";" +
                            it.Kind + ";" +
                            it.Size.Weight.ToString(CultureInfo.InvariantCulture) + ";" +
                            it.Size.Length.ToString(CultureInfo.InvariantCulture) + ";" +
                            it.Size.Slots + ";" +
                            EscapeCsv(it.Maker != null ? it.Maker.Name : null) + ";" +
                            EscapeCsv(it.Maker != null ? it.Maker.City : null) + ";" +
                            it.RequiredLevel + ";" +
                            it.Price.ToString(CultureInfo.InvariantCulture) + ";" +
                            it.Durability + ";" +
                            it.CreatedAt.ToString("o")
                        );
                    }
                }
                Console.WriteLine("Сохранено в " + _filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка сохранения: " + ex.Message);
            }
        }

        private void LoadFromCsv()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    Console.WriteLine("Файл не найден — начнём с пустой коллекции.");
                    return;
                }

                int count = 0;
                using (var sr = new StreamReader(_filePath, Encoding.UTF8))
                {
                    string line;
                    // читаем заголовок
                    line = sr.ReadLine();
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var cols = SplitCsv(line);
                        if (cols.Count < 14) continue; // строка брак

                        var it = new GameItem();
                        int idx = 0;

                        it.Id = ParseInt(cols[idx++]);
                        it.Name = cols[idx++];
                        it.Description = UnNull(cols[idx++]);
                        it.Rarity = ParseEnumRarity(cols[idx++]);
                        it.Kind = ParseEnumKind(cols[idx++]);
                        it.Size = new ItemSize
                        {
                            Weight = ParseDouble(cols[idx++]),
                            Length = ParseDouble(cols[idx++]),
                            Slots = ParseInt(cols[idx++])
                        };
                        it.Maker = new Crafter
                        {
                            Name = UnNull(cols[idx++]),
                            City = UnNull(cols[idx++])
                        };
                        it.RequiredLevel = ParseInt(cols[idx++]);
                        it.Price = ParseDouble(cols[idx++]);
                        it.Durability = ParseInt(cols[idx++]);
                        it.CreatedAt = ParseDate(cols[idx++]);

                        _items[it.Id] = it;
                        count++;
                    }
                }

                GameItem.FixNextId(_items.Values);
                Console.WriteLine("Загружено: " + count + " эл. из " + _filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка чтения файла: " + ex.Message);
            }
        }

        // ----- парсеры/утилиты -----
        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            bool needQuotes = value.IndexOfAny(new[] { ';', '"', '\t' }) >= 0;
            value = value.Replace("\"", "\"\"");
            return needQuotes ? "\"" + value + "\"" : value;
        }

        private static List<string> SplitCsv(string line)
        {
            var res = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        { sb.Append('"'); i++; }
                        else { inQuotes = false; }
                    }
                    else sb.Append(c);
                }
                else
                {
                    if (c == ';')
                    {
                        res.Add(sb.ToString()); sb.Length = 0;
                    }
                    else if (c == '"') { inQuotes = true; }
                    else sb.Append(c);
                }
            }
            res.Add(sb.ToString());
            return res;
        }

        private static string UnNull(string s) { return s ?? ""; }
        private static int ParseInt(string s)
        {
            int v; return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : 0;
        }
        private static double ParseDouble(string s)
        {
            double v; return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0.0;
        }
        private static DateTime ParseDate(string s)
        {
            DateTime dt; return DateTime.TryParse(s, null, DateTimeStyles.RoundtripKind, out dt) ? dt : DateTime.Now;
        }

        // >>> ВСТАВЛЕННЫЕ ФУНКЦИИ ДЛЯ ENUM <<<
        private static Rarity ParseEnumRarity(string s)
        {
            Rarity r; return Enum.TryParse<Rarity>(s, true, out r) ? r : Rarity.Common;
        }
        private static ItemKind ParseEnumKind(string s)
        {
            ItemKind k; return Enum.TryParse<ItemKind>(s, true, out k) ? k : ItemKind.Misc;
        }
        // <<< КОНЕЦ ВСТАВКИ >>>

        // ----- execute_script -----
        private void ExecuteScript(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) { Console.WriteLine("Нужно имя файла."); return; }
            if (!File.Exists(file)) { Console.WriteLine("Файл не найден."); return; }

            var lines = File.ReadAllLines(file, Encoding.UTF8);
            Console.WriteLine("Скрипт " + file + ", команд: " + lines.Length);

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.StartsWith("execute_script", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Пропускаю execute_script внутри скрипта.");
                    continue;
                }

                Console.WriteLine("> " + line);

                string cmd = line, arg = "";
                int sp = line.IndexOf(' ');
                if (sp >= 0) { cmd = line.Substring(0, sp); arg = line.Substring(sp + 1).Trim(); }
                cmd = cmd.ToLowerInvariant();

                switch (cmd)
                {
                    case "help": PrintHelp(); break;
                    case "info": PrintInfo(); break;
                    case "show": Show(); break;
                    case "insert": Insert(); break;
                    case "update": Update(arg); break;
                    case "remove_key": RemoveKey(arg); break;
                    case "clear": Clear(); break;
                    case "save": SaveToCsv(); break;
                    case "exit": return;
                    case "filter_kind": FilterKind(arg); break;
                    case "group_by_rarity": GroupByRarity(); break;
                    case "count": Console.WriteLine(_items.Count); break;
                    default: Console.WriteLine("Неизвестная команда."); break;
                }
            }
        }

        // ----- ввод объекта по полям -----
        private GameItem ReadItemFromConsole(bool isUpdate, int existingId)
        {
            var it = isUpdate ? new GameItem { Id = existingId, CreatedAt = DateTime.Now }
                              : GameItem.CreateNew();

            // Name (обязательно)
            while (true)
            {
                Console.Write("Название (обяз.): ");
                var s = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(s)) { it.Name = s; break; }
                Console.WriteLine("Поле обязательно.");
            }

            Console.Write("Описание (можно пусто): ");
            it.Description = Console.ReadLine();

            Console.WriteLine("Редкость: 1-Common, 2-Rare, 3-Epic, 4-Legendary");
            it.Rarity = (Rarity)ReadIntInRange(1, 4);

            Console.WriteLine("Тип: 1-Weapon, 2-Armor, 3-Potion, 4-Misc");
            it.Kind = (ItemKind)ReadIntInRange(1, 4);

            Console.Write("Создатель (имя, обяз.): ");
            string makerName;
            while (true)
            {
                makerName = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(makerName)) break;
                Console.WriteLine("Имя обяз.");
            }
            Console.Write("Город (можно пусто): ");
            string makerCity = Console.ReadLine();
            it.Maker = new Crafter { Name = makerName, City = makerCity };

            Console.Write("Требуемый уровень (1..60): ");
            it.RequiredLevel = ReadIntInRange(1, 60);

            Console.Write("Цена (>=0): ");
            it.Price = ReadDoubleMin(0);

            Console.Write("Прочность (0..100): ");
            it.Durability = ReadIntInRange(0, 100);

            Console.Write("Вес (>=0): ");
            var w = ReadDoubleMin(0);
            Console.Write("Длина (>=0): ");
            var l = ReadDoubleMin(0);
            Console.Write("Слоты (>=0): ");
            var sslots = ReadIntInRange(0, 999);
            it.Size = new ItemSize { Weight = w, Length = l, Slots = sslots };

            return it;
        }

        private static int ReadIntInRange(int min, int max)
        {
            while (true)
            {
                var s = Console.ReadLine();
                int v;
                if (int.TryParse(s, out v) && v >= min && v <= max) return v;
                Console.Write("Введите число от " + min + " до " + max + ": ");
            }
        }

        private static double ReadDoubleMin(double min)
        {
            while (true)
            {
                var s = Console.ReadLine();
                double v;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v >= min) return v;
                Console.Write("Введите число >= " + min + ": ");
            }
        }
    }

    // ===== Точка входа =====
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            app.Run(args);
        }
    }
}
