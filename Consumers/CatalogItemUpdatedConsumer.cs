using Catalog.Contracts;
using Common;
using Inventory.Entities;
using MassTransit;
using System.Threading.Tasks;

namespace Inventory.Consumers
{
    public class CatalogItemUpdatedConsumer : IConsumer<CatalogItemUpdated>
    {
        private readonly IEntityRepository<CatalogItem> repository; 

        public CatalogItemUpdatedConsumer(IEntityRepository<CatalogItem> repository)
        {
            this.repository = repository;
        }

        public async Task Consume(ConsumeContext<CatalogItemUpdated> context)
        {
            var message = context.Message;

            var item = await repository.GetAsync(message.itemId);

            if (item == null)
            {
                item = new CatalogItem
                {
                    Id = message.itemId,
                    Name = message.Name,
                    Description = message.Description,
                };

                await repository.CreateAsync(item);
            }
            else
            {
                item.Name = message.Name;
                item.Description = message.Description;

                await repository.UpdateAsync(item);
            }
        }
    }
}
