using System;
using System.Collections.Generic;

namespace WeatherSystem
{
    // Учёт живых зон. Статический, и это сознательный отход от конвенции проекта: остальные
    // реестры здесь — Zenject-сервисы (ISpiderRegistryService).
    //
    // Причина отхода: зоны обязаны работать и в Edit Mode, а у редакторного драйвера
    // контейнера нет. Вариант «Zenject в игре плюс FindObjectsByType в редакторе» дал бы
    // два пути обнаружения и, значит, два поведения — ровно тот класс расхождений, который
    // в этой системе уже дважды стоил дорого (снимок в тикете 02, источник высоты в тикете 01).
    // Один статический список честнее двух путей.
    //
    // Регистрация на OnEnable/OnDisable: OnDisable приходит и при уничтожении объекта,
    // и при выгрузке сцены, и при входе в Play Mode, поэтому зона снимается с учёта сама.
    // Потребитель всё равно обязан пропускать null — порядок вызовов Unity при перезагрузке
    // домена гарантировать нельзя.
    public static class WeatherZoneRegistry
    {
        private static readonly List<WeatherZone> Registered = new List<WeatherZone>();

        // Компаратор закэширован статически, чтобы обход каждого кадра не аллоцировал.
        private static readonly Comparison<WeatherZone> ByPriorityThenNameThenId = ComparePriority;

        public static IReadOnlyList<WeatherZone> Zones => Registered;

        public static void Register(WeatherZone zone)
        {
            if (zone != null && !Registered.Contains(zone))
                Registered.Add(zone);
        }

        public static void Unregister(WeatherZone zone) => Registered.Remove(zone);

        // Тикет 07: порядок обхода при перекрытии зон обязан зависеть от приоритета,
        // а не от того, в каком порядке зоны включились при загрузке сцены — тот порядок
        // невидим автору уровня и нестабилен между запусками.
        //
        // Второй ключ — имя, а не InstanceID. InstanceID стабилен только до перезапуска
        // редактора: картинка молча отличалась бы после рестарта, а это худший род
        // непредсказуемости — работает, потом иначе. Имя видно автору и переживает
        // перезапуск. Побочный эффект честный: переименование зоны может поменять
        // победителя при равном приоритете — ровно тот случай, о котором предупреждает
        // WeatherComposer.
        //
        // Сортировка на месте, каждый вызов, а не по флагу «список изменился»: приоритет
        // правится в инспекторе и должен подхватываться сразу, а цена на десятке зон
        // незаметна.
        public static void SortByPriority()
        {
            Registered.Sort(ByPriorityThenNameThenId);
        }

        private static int ComparePriority(WeatherZone a, WeatherZone b)
        {
            if (a == null || b == null)
                return 0;

            int priorityCompare = a.Priority.CompareTo(b.Priority);
            if (priorityCompare != 0)
                return priorityCompare;

            int nameCompare = string.CompareOrdinal(a.name, b.name);
            if (nameCompare != 0)
                return nameCompare;

            return a.GetInstanceID().CompareTo(b.GetInstanceID());
        }
    }
}
