using Catalog.Contracts;
using Common;
using Inventory.Entities;
using MassTransit;
using System.Threading.Tasks;

namespace Inventory.Consumers
{
    public class CatalogItemCreatedConsumer : IConsumer<CatalogItemCreated>
    {
        private readonly IEntityRepository<CatalogItem> repository; 

        public CatalogItemCreatedConsumer(IEntityRepository<CatalogItem> repository)
        {
            this.repository = repository;
        }
        public async Task Consume(ConsumeContext<CatalogItemCreated> context)
        {
            var message = context.Message;

            var item = await repository.GetAsync(message.itemId);

            if (item != null)
                return;

            item = new CatalogItem
            {
                Id = message.itemId,
                Name = message.Name,
                Description = message.Description,
            };

            await repository.CreateAsync(item);
        }
    }
}
