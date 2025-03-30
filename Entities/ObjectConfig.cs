using SQLite;
using UnityEngine;

namespace Mapper.Entities
{
    public class ObjectConfig : ModKit.ORM.ModEntity<ObjectConfig>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        
        public int AreaId { get; set; }
        public int ItemId { get; set; }
        public int ModelId { get; set; }

        public string Position { get; set; }
        [Ignore]
        public Vector3 VPosition
        {
            get => Vector3Converter.ReadJson(Position);
            set => Position = Vector3Converter.WriteJson(value);
        }

        public string Rotation { get; set; }
        [Ignore]
        public Quaternion QRotation
        {
            get => RotationConverter.ReadJson(Rotation);
            set => Rotation = RotationConverter.WriteJson(value);
        }

    }
}
