using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using StackExchange.Redis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Controller {
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase {
        public readonly IMongoDatabase _MongoClient;
        public readonly IDatabase _RedisClient;
        public readonly Data.ECommerceDbContext _eCommerceDbContext;
        public ShopController(IMongoDatabase mongoClient, IDatabase connectionMultiplexer, Data.ECommerceDbContext eCommerceDbContext)
        {
            _MongoClient = mongoClient;
            _RedisClient = connectionMultiplexer;
            _eCommerceDbContext = eCommerceDbContext;
        }
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var red = await  _RedisClient.StringGetAsync("products");
            if (!red.IsNullOrEmpty)
            {
                return Ok(red.ToJson());   
            }
            var mon = _MongoClient.GetCollection<BsonDocument>("products");
            if (!mon.Equals(null))
            {
                await _RedisClient.StringSetAsync("products", mon.ToString());
                return Ok(mon.ToJson());
            }
            var pst = await _eCommerceDbContext.Products.FirstAsync();
            return Ok(pst);
        }
    }
}