using System.Collections.ObjectModel;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace eft_dma_radar
{
    internal static class TarkovDevManager
    {
        /// <summary>
        /// Contains all Tarkov Loot, Quests Items and Tasks mapped via BSGID String.
        /// </summary>
        private static readonly Dictionary<string, LootItem> _allItems = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, QuestItems> _allQuestItems = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Tasks> _allTasks = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Containers> _allLootContainers = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Maps> _allMaps = new(StringComparer.OrdinalIgnoreCase);

        private static readonly string FileName = "api_tarkov_dev_items.json";
        private static bool DataFileExists => File.Exists(FileName);

        public static ReadOnlyDictionary<string, LootItem> AllItems => new(_allItems);
        public static ReadOnlyDictionary<string, QuestItems> AllQuestItems => new(_allQuestItems);
        public static ReadOnlyDictionary<string, Tasks> AllTasks => new(_allTasks);
        public static ReadOnlyDictionary<string, Containers> AllLootContainers => new(_allLootContainers);
        public static ReadOnlyDictionary<string, Maps> AllMaps => new(_allMaps);
        #region Static_Constructor
        static TarkovDevManager()
        {
            LoadData();
        }
        #endregion

        #region Private_Methods
        private static void LoadData()
        {
            TarkovDevResponse jsonResponse;

            if (ShouldFetchDataFromApi())
                jsonResponse = FetchDataFromApi();
            else
                jsonResponse = LoadDataFromFile();

            if (jsonResponse is not null)
            {
                ProcessItems(jsonResponse.data.items);
                ProcessTasks(jsonResponse.data.tasks);
                ProcessQuestItems(jsonResponse.data.questItems);
                ProcessLootContainers(jsonResponse.data.lootContainers);
                ProcessMaps(jsonResponse.data.maps);
            }
        }

        private static bool ShouldFetchDataFromApi()
        {
            return !TarkovDevManager.DataFileExists || File.GetLastWriteTime(FileName).AddHours(6) < DateTime.Now;
        }

        private static TarkovDevResponse FetchDataFromApi()
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(20);

                var body = new
                {
                    query = @"query {
                                    items(lang:zh) {
                                        id
                                        name
                                        shortName
                                        normalizedName
                                        basePrice
                                        avg24hPrice
                                        low24hPrice
                                        high24hPrice
                                        sellFor {
                                        price
                                        vendor {
                                            normalizedName
                                        }
                                        }
                                        category {
                                        id
                                        name
                                        normalizedName
                                        }
                                        weight
                                        categories {
                                        id
                                        name
                                        normalizedName
                                        }
                                    }
                                    tasks(lang:zh) {
                                        id
                                        name
                                        objectives {
                                            id
                                            type
                                            description
                                            maps {
                                                id
                                                name
                                                normalizedName
                                            }
                                            ... on TaskObjectiveItem {
                                                items {
                                                id
                                                name
                                                shortName
                                                }
                                                zones {
                                                id
                                                map {
                                                    id
                                                    normalizedName
                                                    name
                                                }
                                                position {
                                                    y
                                                    x
                                                    z
                                                }
                                                }
                                                requiredKeys {
                                                id
                                                name
                                                shortName
                                                }
                                                count
                                                foundInRaid
                                            }
                                            ... on TaskObjectiveMark {
                                                id
                                                description
                                                markerItem {
                                                id
                                                name
                                                shortName
                                                }
                                                maps {
                                                id
                                                normalizedName
                                                name
                                                }
                                                zones {
                                                id
                                                map {
                                                    id
                                                    normalizedName
                                                    name
                                                }
                                                position {
                                                    y
                                                    x
                                                    z
                                                }
                                                }
                                                requiredKeys {
                                                id
                                                name
                                                shortName
                                                }
                                            }
                                            ... on TaskObjectiveQuestItem {
                                                id
                                                description
                                                requiredKeys {
                                                id
                                                name
                                                shortName
                                                }
                                                maps {
                                                id
                                                normalizedName
                                                name
                                                }
                                                zones {
                                                id
                                                map {
                                                    id
                                                    normalizedName
                                                    name
                                                }
                                                position {
                                                    y
                                                    x
                                                    z
                                                }
                                                }
                                                requiredKeys {
                                                id
                                                name
                                                shortName
                                                }
                                                questItem {
                                                    id
                                                    name
                                                    shortName
                                                    normalizedName
                                                    description
                                                }
                                                count
                                            }
                                            ... on TaskObjectiveBasic {
                                                id
                                                description
                                                requiredKeys {
                                                id
                                                name
                                                shortName
                                                }
                                                maps {
                                                id
                                                normalizedName
                                                name
                                                }
                                                zones {
                                                id
                                                map {
                                                    id
                                                    normalizedName
                                                    name
                                                }
                                                position {
                                                    y
                                                    x
                                                    z
                                                }
                                                }
                                                requiredKeys {
                                                id
                                                name
                                                shortName
                                                }
                                            }
                                        }
                                    }
                                    questItems(lang:zh) {
                                        id
                                        shortName
                                        name
                                        normalizedName
                                    }
                                    lootContainers(lang:zh) {
                                        id
                                        normalizedName
                                        name
                                    }
                                    maps(lang:zh){
                                        name
                                        extracts {
                                            name
                                            position {
                                                x
                                                y
                                                z
                                            }
                                        }
                                        transits {
                                            id
                                            description
                                            position {
                                                x
                                                y
                                                z
                                            }
                                        }
                                    }
                                }"
                };

                var jsonBody = JsonSerializer.Serialize(body);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                try
                {
                    var response = client.PostAsync("https://api.tarkov.dev/graphql", content).Result;

                    if (response.StatusCode == HttpStatusCode.RequestTimeout)
                        throw new FileNotFoundException($"Tarkov.Dev API request failed!");

                    response.EnsureSuccessStatusCode();

                    var responseString = response.Content.ReadAsStringAsync().Result;
                    File.WriteAllText(FileName, responseString);
                    return JsonSerializer.Deserialize<TarkovDevResponse>(responseString);
                }
                catch (Exception ex)
                {
                    try
                    {
                        Program.Log("Tarkov.Dev API request failed - attempting fall back to file");

                        if (!TarkovDevManager.DataFileExists)
                            throw new FileNotFoundException($"The data file '{FileName}' doesn't exist!");

                        return TarkovDevManager.LoadDataFromFile();
                    }
                    catch
                    {
                        throw new FileNotFoundException($"Tarkov.Dev API request failed & the data file '{FileName}' doesn't exist!");
                    }
                }
            }
        }

        private static TarkovDevResponse LoadDataFromFile()
        {
            var responseString = File.ReadAllText(FileName);
            return JsonSerializer.Deserialize<TarkovDevResponse>(responseString);
        }

        private static void ProcessItems(List<TarkovItem> items)
        {
            foreach (var tarkovItem in items)
            {
                _allItems.TryAdd(
                    tarkovItem.id,
                    new LootItem()
                    {
                        ID = tarkovItem.id,
                        Name = tarkovItem.name,
                        Item = tarkovItem,
                        Value = TarkovDevManager.GetItemValue(tarkovItem)
                    }
                );
            }
        }

        private static void ProcessTasks(List<TarkovTasks> tasks)
        {
            foreach (var task in tasks)
            {
                var newTask = new Tasks()
                {
                    Name = task.name,
                    ID = task.id,
                    Objectives = new List<Tasks.Objective>()
                };

                foreach (var objective in task.objectives)
                {
                    var newObjective = new Tasks.Objective()
                    {
                        Description = objective.description,
                        Type = objective.type,
                        ID = objective.id,
                        Maps = objective.maps?
                        .Where(m => m is not null)
                        .Select(m => new ObjectiveMaps
                        {
                            id = m.id,
                            name = m.name,
                            normalizedName = m.normalizedName
                        }).ToList(),
                        Zones = objective.zones?.Select(z => new ObjectiveZones
                        {
                            id = z.id,
                            map = new ObjectiveZones.Map
                            {
                                id = z.map.id,
                                name = z.map.name,
                                normalizedName = z.map.normalizedName
                            },
                            position = new ObjectiveZones.Position
                            {
                                x = z.position.x,
                                y = z.position.y,
                                z = z.position.z
                            }
                        }).ToList(),
                        Items = objective.items?.Select(i => new ObjectiveItem
                        {
                            Id = i.id
                        }).ToList(),

                        Count = objective.count,
                        FoundInRaid = objective.foundInRaid
                    };

                    if (objective.questItem is not null)
                    {
                        newObjective.QuestItem = new ObjectiveItem
                        {
                            Id = objective.questItem.id,
                            Name = objective.questItem.name,
                            ShortName = objective.questItem.shortName,
                            NormalizedName = objective.questItem.normalizedName,
                            Description = objective.questItem.description,
                        };
                    }

                    switch (objective.type)
                    {
                        case "findQuestItem":
                            break;
                        case "visit":
                            break;
                        case "mark":
                            break;
                    }

                    newTask.Objectives.Add(newObjective);
                }

                _allTasks.TryAdd(task.id, newTask);
            }
        }

        private static void ProcessQuestItems(List<TarkovQuestItems> questItems)
        {
            foreach (var questItem in questItems)
            {
                _allQuestItems.TryAdd(
                    questItem.id,
                    new QuestItems()
                    {
                        Name = questItem.name,
                        ID = questItem.id,
                        ShortName = questItem.shortName
                    }
                );
            }
        }

        private static void ProcessLootContainers(List<TarkovContainer> lootContainers)
        {
            foreach (var lootContainer in lootContainers)
            {
                _allLootContainers.TryAdd(
                    lootContainer.id,
                    new Containers()
                    {
                        Name = lootContainer.name,
                        ID = lootContainer.id,
                        NormalizedName = lootContainer.normalizedName
                    }
                );
            }
        }

        private static void ProcessMaps(List<TarkovMap> maps)
        {
            foreach (var map in maps)
            {
                var newMap = new Maps()
                {
                    name = map.name,
                    extracts = new List<Maps.Extract>(),
                    transits = new List<Maps.MapTransit>()
                };

                foreach (var extract in map.extracts)
                {
                    var newExtract = new Maps.Extract()
                    {
                        name = extract.name,
                        position = new Vector3(extract.position.x, extract.position.y, extract.position.z)
                    };

                    newMap.extracts.Add(newExtract);
                };

                foreach (var transit in map.transits)
                {
                    var newTransit = new Maps.MapTransit()
                    {
                        id = transit.id,
                        description = transit.description,
                        position = new Vector3(transit.position.x, transit.position.y, transit.position.z)
                    };

                    newMap.transits.Add(newTransit);
                };

                _allMaps.TryAdd(newMap.name, newMap);
            }
        }

        public static string FormatNumber(int num)
        {
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.##") + "M";
            else if (num >= 1000)
                return (num / 1000D).ToString("0") + "K";
            else
                return num.ToString();
        }

        public static int GetItemValue(TarkovItem item)
        {
            var bestPrice = item.avg24hPrice ?? 0;
            var useTraderPrices = Program.Config.TraderPrices;

            foreach (var vendor in item.sellFor)
            {
                var isFleaMarket = vendor.vendor.normalizedName.Equals("flea-market", StringComparison.OrdinalIgnoreCase);
                if (vendor.price > bestPrice)
                {
                    if (useTraderPrices && isFleaMarket)
                        continue;

                    bestPrice = vendor.price;
                }
            }

            return bestPrice;
        }
        #endregion
    }
    #region Classes
    /// <summary>
    /// New Class to hold Tarkov Items Data.
    /// </summary>
    public class TarkovItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public string shortName { get; set; }
        public string normalizedName { get; set; }
        public int basePrice { get; set; }
        public int? avg24hPrice { get; set; }
        public int? low24hPrice { get; set; }
        public int? high24hPrice { get; set; }
        public double weight { get; set; } 
        public List<Category> categories { get; set; } = new List<Category>();
        public List<VendorPrice> sellFor { get; set; } = new List<VendorPrice>();

        public class Vendor
        {
            public string normalizedName { get; set; }
        }

        public class VendorPrice
        {
            public Vendor vendor { get; set; }
            public int price { get; set; }
        }

        public class Category
        {
            public string id { get; set; }
            public string name { get; set; }
            public string normalizedName { get; set; }
        }
    }

    public class TarkovTasks
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<Objective> objectives { get; set; } = new List<Objective>();

        public class Objective
        {
            public string id { get; set; }
            public string description { get; set; }
            public string type { get; set; }
            public List<ObjectiveMaps>? maps { get; set; } 
            public List<ObjectiveZones>? zones { get; set; } 
            public int? count { get; set; }
            public bool? foundInRaid { get; set; }
            public List<ObjectiveRequiredItem>? items { get; set; }
            public ObjectiveQuestItem questItem { get; set; }
        }
    }

    public class ObjectiveRequiredItem
    {
        public string id { get; set; }
    }

    public class ObjectiveQuestItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public string shortName { get; set; }
        public string normalizedName { get; set; }
        public string description { get; set; }
    }

    public class ObjectiveZones
    {
        public string id { get; set; }
        public Map map { get; set; } 
        public Position position { get; set; } 

        public class Map
        {
            public string id { get; set; }
            public string normalizedName { get; set; }
            public string name { get; set; }
        }

        public class Position
        {
            public double? y { get; set; }
            public double? x { get; set; }
            public double? z { get; set; }
        }
    }

    public class ObjectiveMaps
    {
        public string id { get; set; }
        public string name { get; set; }
        public string normalizedName { get; set; }
    }

    public class TarkovQuestItems
    {
        public string id { get; set; }
        public string name { get; set; }
        public string shortName { get; set; }
        public string normalizedName { get; set; }
    }

    public class TarkovContainer
    {
        public string id { get; set; }
        public string name { get; set; }
        public string normalizedName { get; set; }
    }

    public class Containers
    {
        public string ID { get; set; }
        public string NormalizedName { get; set; }
        public string Name { get; set; }
    }

    public class QuestItems
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string ShortName { get; set; }
    }

    public class ObjectiveItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string NormalizedName { get; set; }
        public string Description { get; set; }
    }

    public class TarkovMap
    {
        public string name { get; set; }
        public List<ExtractInfo> extracts { get; set; }
        public List<TransitInfo> transits { get; set; }

        public class ExtractInfo
        {
            public string name { get; set; }
            public Position position { get; set; }

            public class Position
            {
                public float x { get; set; }
                public float y { get; set; }
                public float z { get; set; }
            }
        }

        public class TransitInfo
        {
            public string id { get; set; }
            public string description { get; set; }
            public Position position { get; set; }

            public class Position
            {
                public float x { get; set; }
                public float y { get; set; }
                public float z { get; set; }
            }
        }
    }

    public class Maps
    {
        public string name { get; set; }
        public List<Extract> extracts { get; set; }
        public List<MapTransit> transits { get; set; }

        public class Extract
        {
            public string name { get; set; }
            public Vector3 position { get; set; }

        }

        public class MapTransit
        {
            public string id { get; set; }
            public string description { get; set; }
            public Vector3 position { get; set; }
        }
    }

    public class Tasks
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public List<Objective> Objectives { get; set; } = new List<Objective>();

        public class Objective
        {
            public string Description { get; set; }
            public string Type { get; set; }
            public string ID { get; set; }
            public List<ObjectiveItem> Items { get; set; }
            public List<ObjectiveMaps> Maps { get; set; } = new List<ObjectiveMaps>();
            public List<ObjectiveZones> Zones { get; set; } = new List<ObjectiveZones>();
            public ObjectiveItem QuestItem { get; set; }
            public int? Count { get; set; }
            public bool? FoundInRaid { get; set; }
        }
    }

    public class TarkovDevResponse
    {
        public TarkovDevData data { get; set; }
    }

    public class TarkovDevData
    {
        public List<TarkovItem> items { get; set; }
        public List<TarkovTasks> tasks { get; set; } 
        public List<TarkovQuestItems> questItems { get; set; }
        public List<TarkovContainer> lootContainers { get; set; }
        public List<TarkovMap> maps { get; set; }
    }
    #endregion
}
