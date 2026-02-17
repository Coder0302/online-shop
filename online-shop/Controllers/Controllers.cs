using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using StackExchange.Redis;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Neo4j.Driver;
using project.Services;
using project.Models.Neo4jModels;
using System.Data.Common;
using ECommerce.Data.Entities.Catalog;

namespace ECommerce.Controller {
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase {
        public INeo4jService _neo4jService;
        public readonly IMongoDatabase _MongoClient;
        public readonly IDatabase _RedisClient;
        public readonly Data.ECommerceDbContext _eCommerceDbContext;
        public readonly IDriver _neo4jDriver;
        public ShopController(IMongoDatabase mongoClient, IDatabase connectionMultiplexer, Data.ECommerceDbContext eCommerceDbContext, IDriver neo4jDriver, INeo4jService neo4jservice)
        {
            _MongoClient = mongoClient;
            _RedisClient = connectionMultiplexer;
            _eCommerceDbContext = eCommerceDbContext;
            _neo4jDriver = neo4jDriver;
            _neo4jService = neo4jservice;
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
        [HttpGet("neo4j/all")]
        public async Task<IActionResult> GetAllNeo4jItems()
        {
            try
            {
                await using var session = _neo4jDriver.AsyncSession();
                var items = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync("""
                        MATCH (n:Item)
                        RETURN n.id as id, 
                               n.name as name, 
                               n.number as number,
                               n.createdAt as createdAt
                        ORDER BY n.number
                        """);
                    
                    var results = new List<Dictionary<string, object>>();
                    await cursor.ForEachAsync(record =>
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["id"] = record["id"].As<string>(),
                            ["name"] = record["name"].As<string>(),
                            ["number"] = record["number"].As<int>(),
                            ["createdAt"] = record["createdAt"]
                        });
                    });
                    return results;
                });
                
                return Ok(new 
                { 
                    success = true, 
                    count = items.Count, 
                    items = items 
                });
            } 
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpPost("neo4j/create-next")]
        public async Task<IActionResult> CreateNextItem([FromBody] CreateItemRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name))
            {
                return BadRequest(new { success = false, error = "Name is required" });
            }
            
            try
            {
                await using var session = _neo4jDriver.AsyncSession();
                
                var result = await session.ExecuteWriteAsync(async tx =>
                {
                    var lastItemCursor = await tx.RunAsync("""
                        MATCH (n:Item)
                        RETURN n.number as lastNumber
                        ORDER BY n.number DESC
                        LIMIT 1
                        """);
                    
                    int nextNumber = 1;
                    if (await lastItemCursor.FetchAsync())
                    {
                        nextNumber = lastItemCursor.Current["lastNumber"].As<int>() + 1;
                    }
                    
                    var newItemId = Guid.NewGuid().ToString();
                    var createCursor = await tx.RunAsync("""
                        CREATE (n:Item {
                            id: $id,
                            name: $name,
                            number: $number,
                            createdAt: datetime()
                        })
                        RETURN n.id as id, n.name as name, n.number as number
                        """,
                        new 
                        { 
                            id = newItemId,
                            name = request.Name,
                            number = nextNumber
                        });
                    
                    return await createCursor.SingleAsync();
                });
                
                return Ok(new 
                { 
                    success = true, 
                    message = $"Created item with number {result["number"]}",
                    item = new 
                    {
                        id = result["id"].As<string>(),
                        name = result["name"].As<string>(),
                        number = result["number"].As<int>()
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }
        
        [HttpDelete("neo4j/delete-first")]
        public async Task<IActionResult> DeleteFirstItem()
        {
            try
            {
                await using var session = _neo4jDriver.AsyncSession();
                
                var deletedItem = await session.ExecuteWriteAsync(async tx =>
                {
                    var findCursor = await tx.RunAsync("""
                        MATCH (n:Item)
                        WITH n ORDER BY n.number ASC
                        LIMIT 1
                        RETURN n.id as id, n.name as name, n.number as number
                        """);
                    
                    if (!await findCursor.FetchAsync())
                    {
                        return null;
                    }
                    
                    var itemData = new
                    {
                        id = findCursor.Current["id"].As<string>(),
                        name = findCursor.Current["name"].As<string>(),
                        number = findCursor.Current["number"].As<int>()
                    };
                    
                    await tx.RunAsync("""
                        MATCH (n:Item {id: $id})
                        DETACH DELETE n
                        """,
                        new { id = itemData.id });
                    
                    return itemData;
                });
                
                if (deletedItem == null)
                {
                    return NotFound(new 
                    { 
                        success = false, 
                        error = "No items found to delete" 
                    });
                }
                
                return Ok(new 
                { 
                    success = true, 
                    message = $"Deleted item with number {deletedItem.number}",
                    deletedItem = deletedItem
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }
        public class UserCommonResult
        {
            public UserNode User { get; set; }
            public int CommonCount { get; set; }
        }
        public class CreateItemRequest
        {
            public string Name { get; set; } = string.Empty;
        }

        public class CreateNodeUser
        {
            public string _id {get;set;} = string.Empty;
            public string _name {get;set;} = string.Empty;
        }
        public class CreateNodeProduct
        {
            public string _id {get;set;} = string.Empty;
            public string _name {get;set;} = string.Empty;
            public string _tags {get;set;} = string.Empty;
        }
        public class CreatetestCreateEdgeVievedNodeUserDto
        {
            public string _id_User {get;set;} = string.Empty;
            public string _id_product {get;set;} = string.Empty;
        }

        /*Чистовая часть*/
        public class DtoSeedData
        {
            public int userCount {get;set;} = 30;
            public int productCount {get;set;} = 50;
            public int storeCount {get;set;} = 10;
        
            public double viewedProb {get;set;} = 0.35;
            public double likedProb {get;set;} = 0.35;
            public double purchasedProb {get;set;} = 0.35;
            public double boughtTogetherProb {get;set;} = 0.35;
            public double visitedProb {get;set;} = 0.35;
            public double quantityProb {get;set;} = 0.35;
            public double shownProb {get;set;} = 0.35;
        }

        public class DtoUserMinMaxDepth
        {
            public string UserId {get;set;} = string.Empty;
            public int MinDepth {get;set;}
            public int maxDepth {get;set;}
        }
        public class DtoUserUserMaxDepth
        {
            public string UserId1 {get;set;} = string.Empty;
            public string UserId2 {get;set;} = string.Empty;
            public int maxDepth {get;set;}
        }
        public class DtoUserLimit
        {
            public string UserId {get;set;} = string.Empty;
            public int limit {get;set;}
            public string edgetype {get;set;} = string.Empty;
        }

        public class ProductCountResult
        {
            public ProductNode Product { get; set; }
            public int Count { get; set; }
        }
        public class ProductTagStatistik
        {
            public string Tag { get; set; }
            public int ProductCount { get; set; }
            public int TotalViews { get; set; }
        }

        [HttpPost("testCreateNodeUser")]
        public async Task<IActionResult> testCreateNodeUser([FromBody] CreateNodeUser Node)
        {
            var User = new UserNode
            {
                Id = Node._id,
                Name = Node._name ?? string.Empty
            };
            await _neo4jService.CreateNodeAsync(User);
            return Ok(new 
                { 
                    success = true, 
                    message = $"Create item"
                });
        }
        [HttpPost("testCreateNodeProduct")]
        public async Task<IActionResult> testCreateNodeProduct([FromBody] CreateNodeProduct Node)
        {
            var Product = new ProductNode
            {
                Id = Node._id,
                Name = Node._name ?? string.Empty,
                Tags = Node._tags.Split(' ').ToList()
            };
            await _neo4jService.CreateNodeAsync(Product);
            return Ok(new 
                { 
                    success = true, 
                    message = $"Create item"
                });
        }
        [HttpGet("testGetNodeUser")]
        public async Task<IActionResult> testGetNodeUser([FromQuery] string id )
        {
            var Node = new VoidNode
            {
                Id = id,
            };
            var rezult =  await _neo4jService.GetNodeAsync(Node);

            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"User with '{id}' not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get item",
                    data = rezult
                });
        }
        [HttpPost("testUpdateNodeUser")]
        public async Task<IActionResult> testUpdateNodeUser([FromBody] CreateNodeUser node)
        {
            var Node = new VoidNode
            {
                Id = node._id,
                Name = node._name ?? string.Empty
            };
            if(await _neo4jService.UpdateNodeAsync(Node))
            {
                return Ok(new 
                { 
                    success = true, 
                    message = $"Update item"
                });
            }

            return NotFound(new 
                { 
                    success = false, 
                    message = $"Update item not found"
                });
        }  
        [HttpDelete("testUpdateNodeUser")]
        public async Task<IActionResult> testDeleteNodeUser([FromBody] CreateNodeUser node)
        {
            var Node = new VoidNode
            {
                Id = node._id
            };
            if(await _neo4jService.DeleteNodeAsync(Node))
            {
                return Ok(new 
                { 
                    success = true, 
                    message = $"Delete item"
                });
            }

            return NotFound(new 
                { 
                    success = false, 
                    message = $"Delete item not found"
                });
        }  
        [HttpGet("testGetTypeWithId")]
        public async Task<IActionResult> testGetTypeWithId([FromQuery] string id )
        {
            
            var rezult =  await _neo4jService.GetTypeWithId(id);

            if (rezult == -1)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"User with '{id}' not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get type with Id",
                    data = $"{rezult}"
                });
        }
        [HttpPost("testCreateEdgeVieved")]
        public async Task<IActionResult> testCreateEdge([FromBody] CreatetestCreateEdgeVievedNodeUserDto tempDto)
        {
            var User = new UserNode
            {
                Id = tempDto._id_User
            };
            var Product = new ProductNode
            {
                Id = tempDto._id_product
            };
            var edgeV = new ViewedEdge();

            await _neo4jService.CreateEdgeAsync<ViewedEdge, UserNode, ProductNode>(User, Product, edgeV);
            return Ok(new 
                { 
                    success = true, 
                    message = $"Create item"
                });
        }
        [HttpPost("testGetEdgeVieved")]
        public async Task<IActionResult> testGetEdge([FromBody] CreatetestCreateEdgeVievedNodeUserDto tempDto)
        {
            var User = new UserNode
            {
                Id = tempDto._id_User
            };
            var Product = new ProductNode
            {
                Id = tempDto._id_product
            };
            var edgeV = new ViewedEdge();

            var rezult = await _neo4jService.GetEdgesAsync<ViewedEdge, UserNode, ProductNode>(User, Product);
            return Ok(new 
                { 
                    success = true, 
                    message = rezult
                });
        }
        [HttpPost("testDeleteEdgeVieved")]
        public async Task<IActionResult> testDeleteEdge([FromBody] CreatetestCreateEdgeVievedNodeUserDto tempDto)
        {
            var User = new UserNode
            {
                Id = tempDto._id_User
            };
            var Product = new ProductNode
            {
                Id = tempDto._id_product
            };
            var edgeV = new ViewedEdge();

            var rezult = await _neo4jService.DeleteEdgeAsync<ViewedEdge, UserNode, ProductNode>(null, User, Product);
            return Ok(new 
                { 
                    success = true, 
                    message = rezult
                });
        }
        
        /*ЧИСТОВАЯ ЧАСТЬ*/
        [HttpDelete("AllClearDB")]
        public async Task<IActionResult> AllClearDB()
        {
            var rezult = await _neo4jService.ClearDatabaseAsync();
            return Ok(new 
                { 
                    success = true, 
                    message = rezult
                });
        }

        [HttpPost("SeedTestData")]
        public async Task<IActionResult> SeedTestData([FromBody] DtoSeedData SeedData)
        {
            await _neo4jService.SeedTestDataAsync(
                SeedData.userCount, SeedData.productCount, SeedData.storeCount,
                SeedData.viewedProb, SeedData.likedProb, SeedData.purchasedProb,
                SeedData.boughtTogetherProb, SeedData.visitedProb, SeedData.quantityProb,
                SeedData.shownProb);
            return Ok(new 
                { 
                    success = true
                });
        }
        [HttpGet("GetNodeByType")]
        public async Task<IActionResult> GetNodeByType([FromQuery] string type )
        {
            object rezult = type.ToLower() switch
                {
                    "user" => await _neo4jService.GetNodesByTypeAsync<UserNode>("User"),
                    "product" => await _neo4jService.GetNodesByTypeAsync<ProductNode>("Product"),
                    "store" => await _neo4jService.GetNodesByTypeAsync<StoreNode>("Store"),
                    _ => throw new ArgumentException($"Unknown node type: {type}")
                };
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"User with '{type}' not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get item",
                    data = rezult
                });
        }

        [HttpGet("GetViewedProductsByUser")]
        public async Task<IActionResult> GetViewedProductsByUser([FromQuery] string userid )
        {
            var user = new UserNode{Id = userid};
            var rezult = await _neo4jService.GetViewedProductsByUserAsync(user);
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"User with '{userid}' not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get items",
                    data = rezult
                });
        }
        [HttpGet("GetUsersWhoLikedProductAsync")]
        public async Task<IActionResult> GetUsersWhoLikedProductAsync([FromQuery] string productid )
        {
            var product = new ProductNode{Id = productid};
            var rezult = await _neo4jService.GetUsersWhoLikedProductAsync(product);
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"User with '{productid}' not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get items",
                    data = rezult
                });
        }

        [HttpGet("GetRecommendedProductsbyUserAsync")]
        public async Task<IActionResult> GetRecommendedProductsbyUserAsync([FromQuery] string userid )
        {
            var user = new UserNode{Id = userid};
            var rezult = await _neo4jService.GetRecommendedProductsbyUserAsync(user);
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"User with '{userid}' not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get items",
                    data = rezult
                });
        }
        [HttpGet("GetRecommendedProductsbyProductAsync")]
        public async Task<IActionResult> GetRecommendedProductsbyProductAsync([FromQuery] string productid )
        {
            var product = new ProductNode{Id = productid};
            var rezult = await _neo4jService.GetRecommendedProductsbyProductAsync(product);
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"User with '{productid}' not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get items",
                    data = rezult
                });
        }
        [HttpGet("GetConnectedUsers")]
        public async Task<IActionResult> GetConnectedUsers([FromQuery] DtoUserMinMaxDepth dto )
        {
            var user = new UserNode{Id = dto.UserId};
            var rezult = await _neo4jService.GetConnectedUsersAsync(user, dto.MinDepth, dto.maxDepth);
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"User with '{user.Id}' not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get items",
                    data = rezult
                });
        }
        [HttpGet("GetUserConnectionsWithDepth")]
        public async Task<IActionResult> GetUserConnectionsWithDepth([FromQuery] DtoUserMinMaxDepth dto )
        {
            var user = new UserNode{Id = dto.UserId};
            var rezult = await _neo4jService.GetUserConnectionsWithDepthAsync(user, dto.maxDepth, dto.MinDepth);
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"User with '{user.Id}' not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get items",
                    data = rezult
                });
        }
        [HttpGet("FindPathBetweenUsers")]
        public async Task<IActionResult> FindPathBetweenUsers([FromQuery] DtoUserUserMaxDepth dto )
        {
            var user1 = new UserNode{Id = dto.UserId1};
            var user2 = new UserNode{Id = dto.UserId2};
            var rezult = await _neo4jService.FindPathBetweenUsersAsync(user1, user2, dto.maxDepth);
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"User with '{user1.Id}' not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get items",
                    data = rezult
                });
        }

        [HttpGet("GetUsersWithCommonEdge")]
        public async Task<IActionResult> GetUsersWithCommonEdge([FromQuery] DtoUserLimit dto)
        {
            var user = new UserNode{Id = dto.UserId};
            var fartory = new Neo4jEdgeFactory();
            var edge = fartory.CreateEdgeByStringType(dto.edgetype);
            var rezult = await _neo4jService.GetUsersWithCommonEdgeAsync(user, edge, dto.limit);
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"User with '{user.Id}' not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get items",
                    data = rezult
                });
        }
        [HttpGet("GetTopProducts")]
        public async Task<IActionResult> GetTopProducts([FromQuery] string edgetype)
        {
            var rezult = await _neo4jService.GetTopProductsAsync(edgetype);
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get items",
                    data = rezult
                });
        }
        [HttpGet("GetTagStatistics")]
        public async Task<IActionResult> GetTagStatistics()
        {
            var rezult = await _neo4jService.GetTagStatisticsAsync();
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get items",
                    data = rezult
                });
        }
        public class UserPurchaseSummary
        {
            public UserNode User { get; set; }
            public List<ProductNode> Products { get; set; } = new();
            public int TotalPurchases { get; set; }
            public List<string> ProductIds { get; set; } = new();
        }
        [HttpGet("GetTopUsersByViewedAndPurchased")]
        public async Task<IActionResult> GetTopUsersByViewedAndPurchased([FromQuery] int limit)
        {
            var rezult = await _neo4jService.GetTopUsersByViewedAndPurchasedAsync(limit);
            if (rezult == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"not found"
                });
            }
           
            return Ok(new 
                { 
                    success = true, 
                    message = $"Get items",
                    data = rezult
                });
        }
    }
}
