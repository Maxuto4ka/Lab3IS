using System;
using System.Collections.Generic;
using System.Linq;

// Структура для інформації про викладача
public struct ProfessorInfo
{
    public List<string> Subjects;
    public int MaxHours;
    public int CurrentHours;
}
// Структура для розкладу
public struct Schedule
{
    public string Group;
    public string Weekday;
    public string Time;
    public string Professor;
    public string Subject;
    public string Room;
}

public class Program
{
    // Оголошення констант
    static readonly List<string> Subjects = new List<string> { "English", "history", "math", "science", "literature", "biology", "I.T", "physics", "chemistry", "geography" };
    static readonly List<string> Groups = new List<string> { "G1", "G2", "G3" };
    static readonly Dictionary<string, List<int>> GroupPrograms = new Dictionary<string, List<int>>
    {
        { "G1", new List<int> { 4, 2, 2, 1, 2, 1, 0, 2, 0, 1} },
        { "G2", new List<int> { 2, 1, 5, 2, 0, 1, 0, 2, 1, 1 } },
        { "G3", new List<int> { 2, 1, 1, 1, 0, 3, 3, 0, 3, 1 } }
    };
    static readonly List<string> Rooms = new List<string> { "A1", "A2", "A3" };
    static readonly List<string> Times = new List<string> { "8:40-10:15", "10:35-12:10", "12:20-13:55" };
    static readonly List<string> Weekdays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };

    static Dictionary<string, List<int>> GroupSubjectCount = new Dictionary<string, List<int>>();

    static void InitializeGroupSubjectCount()
    {
        GroupSubjectCount = GroupPrograms.ToDictionary(
            entry => entry.Key,
            entry => new List<int>(new int[Subjects.Count])
        );
    }

    // Генерація розкладу
    static Dictionary<(string, string, string), (string, string, string)> CreateSchedule(Dictionary<string, ProfessorInfo> professorsInfo)
    {
        var schedule = new Dictionary<(string, string, string), (string, string, string)>();
        Random rand = new Random();

        foreach (var group in Groups)
        {
            foreach (var weekday in Weekdays)
            {
                foreach (var time in Times)
                {
                    var professor = professorsInfo.ElementAt(rand.Next(professorsInfo.Count)).Key;
                    var subject = professorsInfo[professor].Subjects[rand.Next(professorsInfo[professor].Subjects.Count)];
                    var room = Rooms[rand.Next(Rooms.Count)];

                    var key = (group, weekday, time);
                    schedule[key] = (professor, subject, room);
                }
            }
        }

        return schedule;
    }

    // Оцінка якості розкладу
    static int Heuristic(Dictionary<(string, string, string), (string, string, string)> schedule)
    {
        int cost = 0;
        var professorHours = new Dictionary<string, int>();
        var professorTimeConflict = new Dictionary<(string professor, string weekday, string time), int>();
        var roomUsage = new Dictionary<(string, string), List<string>>();
        var groupSubjectCount = Groups.ToDictionary(g => g, g => new int[Subjects.Count]);

        foreach (var entry in schedule)
        {
            var key = entry.Key; // (group, weekday, time)
            var value = entry.Value; // (professor, subject, room)
            var group = key.Item1;
            var weekday = key.Item2;
            var time = key.Item3;
            var professor = value.Item1;
            var subject = value.Item2;
            var room = value.Item3;

            // Перевірка перевищення годин викладача
            if (!professorHours.ContainsKey(professor))
                professorHours[professor] = 0;

            professorHours[professor]++;
            if (professorHours[professor] > 3)
                cost += 5;

            // Перевірка, чи викладач проводить більше ніж одну пару в один і той самий час
            var professorTimeKey = (professor, weekday, time);
            if (!professorTimeConflict.ContainsKey(professorTimeKey))
                professorTimeConflict[professorTimeKey] = 0;

            professorTimeConflict[professorTimeKey]++;
            if (professorTimeConflict[professorTimeKey] > 1)
            {
                cost += 10;
            }

            // Перевірка використання аудиторії кількома групами одночасно
            var roomKey = (weekday, time);
            if (!roomUsage.ContainsKey(roomKey))
                roomUsage[roomKey] = new List<string>();

            if (roomUsage[roomKey].Contains(room))
                cost += 10;
            else
                roomUsage[roomKey].Add(room);

            // Відслідковування кількості занять по предметах для кожної групи
            int subjectIndex = Subjects.IndexOf(subject);
            if (subjectIndex >= 0)
            {
                groupSubjectCount[group][subjectIndex]++;
            }
        }
        // Штраф за невідповідність кількості пар предметів програмі групи
        foreach (var group in Groups)
        {
            for (int i = 0; i < Subjects.Count; i++)
            {
                int actual = groupSubjectCount[group][i];
                int expected = GroupPrograms[group][i];
                cost += Math.Abs(actual - expected) * 5; // Штраф за кожну невідповідність
            }
        }

        return cost;
    }


    // Схрещування двох розкладів
    static Dictionary<(string, string, string), (string, string, string)> Crossover(
        Dictionary<(string, string, string), (string, string, string)> schedule1,
        Dictionary<(string, string, string), (string, string, string)> schedule2)
    {
        var child = new Dictionary<(string, string, string), (string, string, string)>(schedule1);
        Random rand = new Random();

        foreach (var pair in schedule1)
        {
            if (schedule2.ContainsKey(pair.Key) && rand.Next(2) == 0)
            {
                child[pair.Key] = schedule2[pair.Key];
            }
        }

        return child;
    }

    // Мутація розкладу
    static void Mutate(ref Dictionary<(string, string, string), (string, string, string)> schedule,
        Dictionary<string, ProfessorInfo> professorsInfo)
    {
        if (schedule.Count == 0) return;

        Random rand = new Random();
        var keyToMutate = schedule.Keys.ElementAt(rand.Next(schedule.Count));
        var professor = professorsInfo.ElementAt(rand.Next(professorsInfo.Count)).Key;

        string subject = professorsInfo[professor].Subjects[rand.Next(professorsInfo[professor].Subjects.Count)];
        string room = Rooms[rand.Next(Rooms.Count)];

        if (!schedule.ContainsKey(keyToMutate) && professorsInfo[professor].CurrentHours < professorsInfo[professor].MaxHours)
        {
            schedule[keyToMutate] = (professor, subject, room);
        }
    }

    // Генетичний алгоритм
    static Dictionary<(string, string, string), (string, string, string)> GeneticAlgorithm(Dictionary<string, ProfessorInfo> professorsInfo)
    {
        Random rand = new Random();
        List<Dictionary<(string, string, string), (string, string, string)>> population = new List<Dictionary<(string, string, string), (string, string, string)>>();

        // Створення початкової популяції
        for (int i = 0; i < 100; ++i)
        {
            population.Add(CreateSchedule(professorsInfo));
        }

        // Кількість ітерацій
        for (int iter = 0; iter < 100; ++iter)
        {
            population.Sort((a, b) => Heuristic(a) - Heuristic(b));

            // Відбір найкращих
            population = population.Take(20).ToList();

            // Створення нового покоління
            for (int i = 0; i < 80; ++i)
            {
                var schedule1 = population[rand.Next(population.Count)];
                var schedule2 = population[rand.Next(population.Count)];
                // Створення нового розкладу шляхом схрещування
                var child = Crossover(schedule1, schedule2);
                Mutate(ref child, professorsInfo);

                population.Add(child);
            }
        }
        // Повернення найкращого розкладу
        return population.OrderBy(s => Heuristic(s)).First();
    }

    // Виведення розкладу
    static void PrintSchedule(Dictionary<(string, string, string), (string, string, string)> schedule)
    {
        foreach (var weekday in Weekdays)
        {
            Console.WriteLine($"\n{weekday}:");
            foreach (var time in Times)
            {
                var pairs = new List<string>();

                foreach (var pair in schedule)
                {
                    var key = pair.Key;
                    if (key.Item2 == weekday && key.Item3 == time)
                    {
                        var professor = pair.Value.Item1;
                        var subject = pair.Value.Item2;
                        var room = pair.Value.Item3;
                        string group = key.Item1;
                        pairs.Add($"Group: {group}, Professor: {professor}, Subject: {subject}, Room: {room}");
                    }
                }
                if (pairs.Count > 0)
                {
                    Console.WriteLine($"Time: {time}");
                    Console.WriteLine(string.Join("; ", pairs));
                }
            }
        }
    }


    public static void Main(string[] args)
    {
        var professorsInfo = new Dictionary<string, ProfessorInfo>
        {
            { "James", new ProfessorInfo { Subjects = new List<string> { "math", "science", "I.T", "physics" }, MaxHours = 3, CurrentHours = 0 } },
            { "Robert", new ProfessorInfo { Subjects = new List<string> { "math", "science", "physics" }, MaxHours = 3, CurrentHours = 0 } },
            { "John", new ProfessorInfo { Subjects = new List<string> { "math", "physics" }, MaxHours = 3, CurrentHours = 0 } },
            { "William", new ProfessorInfo { Subjects = new List<string> { "chemistry", "history", "biology", "literature" }, MaxHours = 3, CurrentHours = 0 } },
            { "Thomas", new ProfessorInfo { Subjects = new List<string> { "science", "chemistry", "biology" }, MaxHours = 3, CurrentHours = 0 } },
            { "Paul", new ProfessorInfo { Subjects = new List<string> { "history", "English", "geography" }, MaxHours = 3, CurrentHours = 0 } },
            { "Kevin", new ProfessorInfo { Subjects = new List<string> { "math", "I.T" }, MaxHours = 3, CurrentHours = 0 } },
            { "Anna", new ProfessorInfo { Subjects = new List<string> { "chemistry", "biology", "science" }, MaxHours = 3, CurrentHours = 0 } },
            { "Sophia", new ProfessorInfo { Subjects = new List<string> { "geography", "English" }, MaxHours = 3, CurrentHours = 0 } },
            { "Olivia", new ProfessorInfo { Subjects = new List<string> { "English", "literature", "history" }, MaxHours = 3, CurrentHours = 0 } },
            { "Emma", new ProfessorInfo { Subjects = new List<string> { "math", "I.T" }, MaxHours = 3, CurrentHours = 0 } },
            { "Charlotte", new ProfessorInfo { Subjects = new List<string> { "history", "geography" }, MaxHours = 3, CurrentHours = 0 } },
            { "Lily", new ProfessorInfo { Subjects = new List<string> { "literature", "English" }, MaxHours = 3, CurrentHours = 0 } }
        };
        InitializeGroupSubjectCount();
        var bestSchedule = GeneticAlgorithm(professorsInfo);
        PrintSchedule(bestSchedule);
    }
}
