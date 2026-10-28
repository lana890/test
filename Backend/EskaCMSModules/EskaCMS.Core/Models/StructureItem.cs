
using System;

namespace EskaCMS.Core.Models
{
    /// <summary>
    /// Abstract class for an hierarchical item in a structure.
    /// </summary>
    [Serializable]
    public abstract class StructureItem<TStructure, T>
        where T : StructureItem<TStructure, T>
        where TStructure : Structure<TStructure, T>
    {
        /// <summary>
        /// Gets/sets the unique id.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets/sets the level in the hierarchy.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Gets/sets the child items.
        /// </summary>
        public TStructure Items { get; set; }
    }
}