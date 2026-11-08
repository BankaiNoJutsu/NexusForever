using System.Reflection;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

// Counts, per (table, prerequisite column), the references to prerequisite ids that contain a slot of the interesting types.
string db = @"I:\tools\extract\DB";
var interesting = new[] { 9, 24, 25, 146, 149, 232 };

var preqBytes = File.ReadAllBytes(Path.Combine(db, "Prerequisite.tbl"));
var prereq = new GameTable<PrerequisiteEntry>(new MemoryStream(preqBytes));
var idsByType = interesting.ToDictionary(t => t, t => new HashSet<uint>());
var valuesByPrereq = new Dictionary<uint, List<(int Type, uint Comparison, uint Object, uint Value)>>();
foreach (PrerequisiteEntry e in prereq.Entries)
{
    if (e == null) continue;
    for (int i = 0; i < e.PrerequisiteTypeId.Length; i++)
    {
        int t = (int)e.PrerequisiteTypeId[i];
        if (idsByType.TryGetValue(t, out var set))
        {
            set.Add(e.Id);
            if (!valuesByPrereq.TryGetValue(e.Id, out var l)) valuesByPrereq[e.Id] = l = new();
            l.Add((t, (uint)e.PrerequisiteComparisonId[i], e.ObjectId[i], e.Value[i]));
        }
    }
}

var rows = new List<string>();
var detail = new List<string>();
var assembly = typeof(PrerequisiteEntry).Assembly;
foreach (Type type in assembly.GetTypes().Where(t => t.Namespace == "NexusForever.GameTable.Model" && t.Name.EndsWith("Entry")))
{
    string name = type.Name[..^"Entry".Length];
    string path = Path.Combine(db, name + ".tbl");
    if (!File.Exists(path)) continue;
    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
        .Where(f => f.Name.StartsWith("Prerequisite") && (f.FieldType == typeof(uint) || f.FieldType == typeof(uint[]))).ToList();
    if (fields.Count == 0) continue;
    object table;
    try
    {
        table = Activator.CreateInstance(typeof(GameTable<>).MakeGenericType(type), new MemoryStream(File.ReadAllBytes(path)));
    }
    catch (Exception ex)
    {
        rows.Add($"LOADFAIL\t{name}\t{ex.GetBaseException().Message}");
        continue;
    }
    var entries = ((System.Collections.IEnumerable)table.GetType().GetProperty("Entries").GetValue(table)).Cast<object>().Where(o => o != null).ToList();
    foreach (var f in fields)
    {
        foreach (var kv in idsByType)
        {
            int count = 0;
            var sample = new List<string>();
            foreach (var entry in entries)
            {
                object v = f.GetValue(entry);
                IEnumerable<uint> values = v is uint[] arr ? arr : new[] { (uint)v };
                foreach (uint id in values)
                {
                    if (id != 0 && kv.Value.Contains(id))
                    {
                        count++;
                        detail.Add($"{kv.Key}	{name}	{f.Name}	{type.GetField("Id")?.GetValue(entry)}	{id}");
                        if (sample.Count < 3) sample.Add(type.GetField("Id")?.GetValue(entry)?.ToString() + ":" + id);
                    }
                }
            }
            if (count > 0) rows.Add($"{kv.Key}\t{name}\t{f.Name}\t{count}\t{string.Join(",", sample)}");
        }
    }
}
File.WriteAllLines(@"I:\tools\prereq_refs_named.tsv", rows);
File.WriteAllLines(@"I:\tools\prereq_refs_detail.tsv", detail);

// value detail for 24/25 prerequisites
var lines = new List<string>();
foreach (var kv in valuesByPrereq.OrderBy(k => k.Key))
    lines.Add(kv.Key + "\t" + string.Join(" ; ", kv.Value.Select(x => $"t{x.Type} c{x.Comparison} o{x.Object} v{x.Value}")));
File.WriteAllLines(@"I:\tools\prereq_values.tsv", lines);
Console.WriteLine("rows " + rows.Count);

// type of the achievements that reference a 146 prerequisite, and how many achievements have each type
{
    var ach = new GameTable<AchievementEntry>(new MemoryStream(File.ReadAllBytes(Path.Combine(db, "Achievement.tbl"))));
    var set146 = idsByType[146];
    var lst = ach.Entries.Where(a => a != null && set146.Contains(a.PrerequisiteIdObjective)).ToList();
    Console.WriteLine("146 achievements: " + lst.Count + " types: " + string.Join(",", lst.Select(a => a.AchievementTypeId).Distinct()));
    int t33 = ach.Entries.Count(a => a != null && a.AchievementTypeId == 33);
    Console.WriteLine("achievements of type 33: " + t33 + " of " + ach.Entries.Count(a => a != null));
}

{
    var cr = new GameTable<Creature2Entry>(new MemoryStream(File.ReadAllBytes(Path.Combine(db, "Creature2.tbl"))));
    var all = cr.Entries.Where(c => c != null).ToList();
    Console.WriteLine("Creature2 rows " + all.Count + ", with PrerequisiteIdVisibility " + all.Count(c => c.PrerequisiteIdVisibility != 0)
        + ", distinct visibility prerequisites " + all.Where(c => c.PrerequisiteIdVisibility != 0).Select(c => c.PrerequisiteIdVisibility).Distinct().Count());
}
