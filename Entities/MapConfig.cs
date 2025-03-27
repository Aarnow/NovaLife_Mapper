using Life.AreaSystem;
using Newtonsoft.Json;
using SQLite;
using System.Collections.Generic;

namespace Mapper.Entities
{
    public class MapConfig : ModKit.ORM.ModEntity<MapConfig>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public int AreaId { get; set; }
        public int CreatedAt { get; set; }
        public int ObjectCount { get; set; }
        public string Source { get; set; }

        public string Objects { get; set; }
        [Ignore]
        public List<LifeObject> ListOfLifeObject { get; set; } = new List<LifeObject>();

        public MapConfig()
        {
        }

        public void SerializeObjects()
        {
            Objects = JsonConvert.SerializeObject(ListOfLifeObject);
        }

        public void DeserializeObjects()
        {
            ListOfLifeObject = JsonConvert.DeserializeObject<List<LifeObject>>(Objects);
        }
    }
}
