using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Storefront
{
    public class Category : ICategory
    {
        public uint Id { get; }
        public string Name { get; }
        public string Description { get; }
        public uint ParentCategoryId { get; }
        public uint Index { get; }
        public bool Visible { get; }

        /// <summary>
        /// Create a new <see cref="ICategory"/> from an existing database model.
        /// </summary>
        public Category(StoreCategoryModel model, uint? parentCategoryIdOverride = null, bool? visibleOverride = null)
        {
            Id               = model.Id;
            Name             = model.Name;
            Description      = model.Description;
            ParentCategoryId = parentCategoryIdOverride ?? model.ParentId;
            Index            = model.Index;
            Visible          = visibleOverride ?? Convert.ToBoolean(model.Visible);
        }

        public ServerStoreCategories.StoreCategory Build()
        {
            return new ServerStoreCategories.StoreCategory
            {
                CategoryId       = Id,
                ParentCategoryId = ParentCategoryId,
                Name             = Name,
                Description      = Description,
                Index            = Index,
                Visible          = Visible
            };
        }
    }
}
