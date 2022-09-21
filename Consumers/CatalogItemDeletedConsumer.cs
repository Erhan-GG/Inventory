using Catalog.Contracts;
using Common;
using Inventory.Entities;
using MassTransit;
using System.Threading.Tasks;

namespace Inventory.Consumers
{
    public class CatalogItemDeletedConsumer : IConsumer<CatalogItemDeleted>
    {
        private readonly IEntityRepository<CatalogItem> repository; 

        public CatalogItemDeletedConsumer(IEntityRepository<CatalogItem> repository)
        {
            this.repository = repository;
        }

        public async Task Consume(ConsumeContext<CatalogItemDeleted> context)
        {
            var message = context.Message;

            var item = await repository.GetAsync(message.itemId);

            if (item == null)
            {
                return;
            }

            await repository.RemoveAsync(item.Id);
        }
    }
}
