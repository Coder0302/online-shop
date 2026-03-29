using project.Models.Neo4jModels;
using Neo4j.Driver;
using project.Models.Neo4jModels.Responses;
using System.Data;
using System.Text.Json;

public class UserCommonResult
{
    public UserNode User { get; set; }
    public int CommonCount { get; set; }
}
public class ProductCountResult
{
    public ProductNode Product { get; set; }
    public int count { get; set; }
}
public class ProductTagStatistik
{
    public string Tag { get; set; }
    public int ProductCount { get; set; }
    public int TotalViews { get; set; }
}
public class UserPurchaseSummary
        {
            public UserNode User { get; set; }
            public List<ProductNode> Products { get; set; } = new();
            public int TotalPurchases { get; set; }
            public List<string> ProductIds { get; set; } = new();
        }
namespace project.Services
{
    public interface INeo4jService
    {
        Task<int> GetTypeWithId(string nodeId);
        Task<bool> CreateNodeAsync<T>(T node) where T : Neo4jNode;
        Task<Dictionary<string, object>> GetNodeAsync<T>(T node) where T : Neo4jNode;
        Task<bool> UpdateNodeAsync<T>(T node) where T : Neo4jNode;
        Task<bool> DeleteNodeAsync<T>(T node) where T : Neo4jNode;
        
        Task<E> CreateEdgeAsync<E, T, Y>(T srcNode, Y dstNode, E Edge) 
            where E : Neo4jEdge where T : Neo4jNode where Y : Neo4jNode;
        Task<List<E>> GetEdgesAsync<E, T, Y>(T NodeSrc, Y NodeDst) where E : Neo4jEdge where T : Neo4jNode where Y : Neo4jNode;
        Task<bool> DeleteEdgeAsync<E, T, Y>(E Edge, T NodeSrc, Y NodeDst) where E: Neo4jEdge where T:Neo4jNode where Y:Neo4jNode;
        
        Task<List<T>> GetNodesByTypeAsync<T>(string nodetype) where T : Neo4jNode;
        Task<QueryResult> ExecuteCypherAsync(string query, object parameters = null);
        
        Task SeedTestDataAsync(
            int users = 30, int products = 50, int stores = 10,
            double viewedProb = 0.35, double likedProb = 0.35,
            double purchasedProb = 0.35, double boughtTogetherProb = 0.35,
            double visitedProb = 0.35, double quantityProb = 0.35,
            double shownProb = 0.35
            );
        Task<List<ProductNode>> GetViewedProductsByUserAsync(UserNode user);
        Task<List<UserNode>> GetUsersWhoLikedProductAsync(ProductNode product);
        Task<List<ProductNode>> GetRecommendedProductsbyUserAsync(UserNode user);
        Task<List<ProductNode>> GetRecommendedProductsbyProductAsync(ProductNode Product);
        Task<bool> ClearDatabaseAsync();
        Task<List<UserNode>> GetConnectedUsersAsync(UserNode user, int minDepth = 2, int maxDepth = 4);
        Task<List<Dictionary<string, object>>> FindPathBetweenUsersAsync(UserNode user1, UserNode user2, int maxDepth = 5);
        Task<List<Dictionary<string, object>>> GetUserConnectionsWithDepthAsync(UserNode user, int maxDepth = 4, int minDepth = 1);
        Task<List<UserCommonResult>> GetUsersWithCommonEdgeAsync(UserNode user, Neo4jEdge edge, int limit = 20);
        Task<List<ProductCountResult>> GetTopProductsAsync(string edgetype, int limit = 10);
        Task<List<ProductTagStatistik>> GetTagStatisticsAsync();
        Task<List<UserPurchaseSummary>> GetTopUsersByViewedAndPurchasedAsync(int limit = 10);
    }
    public class Neo4jService : INeo4jService
    {
        private IDriver _context;

        public Neo4jService(IDriver context)
        {
            _context = context;
        }

        public async Task<bool> test()
        {
            Console.WriteLine("TEST1728");

            return true;
        }

        public async Task<int> GetTypeWithId(string nodeId)
        {

            if(string.IsNullOrEmpty(nodeId) )
                throw new ArgumentNullException(nameof(nodeId));
    
            var query = @"
                MATCH (n {ext_id: $ext_id})
                RETURN properties(n) as properties";
            var parameters = new Dictionary<string, object>
            {
                ["ext_id"] = nodeId
            };
            await using var session = _context.AsyncSession();
            try
            {
                var result = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
            
                    if (await cursor.FetchAsync())
                    {
                        var properties = cursor.Current["properties"].As<Dictionary<string, object>>();
                        return properties;
                    }
                    return null;
                });
                if(result == null)
                    return -1;
                return Convert.ToInt32(result["type"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting node: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CreateNodeAsync<T>(T node) where T : Neo4jNode
        {
            if(node == null)
                throw new ArgumentNullException(nameof(node));
            
            string typename = node.GetStringType();

            var properties = node.ToProperties();
            var query = $@"
                MERGE (n:{typename} {{ext_id: $ext_id}})
                ON CREATE SET n = $createProperties, n.ext_id = $ext_id
                RETURN 
                    n.ext_id as ext_id,
                    n.type as type";
            var parameters = new Dictionary<string, object> { 
                ["ext_id"] = node.Id,
                ["createProperties"] = properties
            };
            await using var session = _context.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var record = await cursor.SingleAsync();

                    node.Id = record["ext_id"].As<string>();
                    return node;
                });
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<Dictionary<string, object>> GetNodeAsync<T>(T node) where T : Neo4jNode
        {
            if(node == null)
                throw new ArgumentNullException(nameof(node));

            string typename = node.GetStringType();
            var query = $@"
                MATCH (n:{typename} {{ext_id: $ext_id}})
                RETURN n.ext_id as ext_id, elementId(n) as elementId, properties(n) as properties";
            var parameters = new Dictionary<string, object>
            {
                ["ext_id"] = node.Id
            };
            await using var session = _context.AsyncSession();
            try
            {
                var result = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
            
                    if (await cursor.FetchAsync())
                    {
                        var id = cursor.Current["ext_id"].As<string>();
                        var properties = cursor.Current["properties"].As<Dictionary<string, object>>();
                        return properties;
                    }
                    return null;
                });

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting node: {ex.Message}");
                throw;
            }
        }
        public async Task<bool> UpdateNodeAsync<T>(T node) where T : Neo4jNode
        {
            if(node == null)
                throw new ArgumentNullException(nameof(node));
            
            string typename = node.GetStringType();

            var properties = node.ToProperties();
            var query = $@"
                MERGE (n:{typename} {{ext_id: $ext_id}})
                ON MATCH SET n += $updateProperties
                RETURN 
                    n.ext_id as ext_id,
                    n.type as type";
            var parameters = new Dictionary<string, object> { 
                ["ext_id"] = node.Id,
                ["updateProperties"] = properties
            };
            await using var session = _context.AsyncSession();
            try
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var record = await cursor.SingleAsync();

                    node.Id = record["ext_id"].As<string>();
                    return node;
                });
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public async Task<bool> DeleteNodeAsync<T>(T node) where T : Neo4jNode
        {
            if(node == null)
                throw new ArgumentNullException(nameof(node));
            string typename = node.GetStringType();
            var query = $@"
                MATCH (n:{typename} {{ext_id: $ext_id}})
                DETACH DELETE n
                RETURN COUNT(n) as deletedCount";

            var parameters = new Dictionary<string, object>
            {
                ["ext_id"] = node.Id
            };

            await using var session = _context.AsyncSession();
            var result = await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(query, parameters);
                var record = await cursor.SingleAsync();
                return record["deletedCount"].As<int>() > 0;
            });

            return result;
        }
        
        public async Task<E> CreateEdgeAsync<E, T, Y>(T nodeSrc, Y nodeDst, E edge) where E : Neo4jEdge where T : Neo4jNode where Y : Neo4jNode
        {
            if(nodeSrc == null || nodeDst == null)
                throw new ArgumentNullException(nameof(nodeSrc));
            if(edge == null)
                throw new ArgumentNullException(nameof(edge));
            if(nodeSrc.Type != edge.TypeNodeSrc || nodeDst.Type != edge.TypeNodeDst)
            {
                Console.WriteLine("invalid edge src dst");
                return null;
            }
            var properties = edge.ToProperties();
            string relationshipType = edge.Name;
            string srcNodeType = nodeSrc.GetStringType();
            string dstNodeType = nodeDst.GetStringType();
            var query = $@"
                MATCH (a:{srcNodeType} {{ext_id: $srcId}})
                MATCH (b:{dstNodeType} {{ext_id: $dstId}})
                MERGE (a)-[r:{relationshipType}]->(b)
                SET r+= $properties
                RETURN r, elementId(r) as elementId";
            var parameters = new Dictionary<string, object>
            {
                ["srcId"] = nodeSrc.Id,
                ["dstId"] = nodeDst.Id,
                ["properties"] = properties
            };
            await using var session = _context.AsyncSession();
            try
            {
                var result = await session.ExecuteWriteAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var record = await cursor.SingleAsync();
                    
                    var relationshipElementId = record["elementId"].As<string>();
                    
                    return edge;
                });
                return result;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error creating edge: {ex.Message}");
                Console.WriteLine($"Query: {query}");
                throw;
            }
        }
        public async Task<List<E>> GetEdgesAsync<E, T, Y>(T NodeSrc, Y NodeDst) 
            where E : Neo4jEdge 
            where T : Neo4jNode 
            where Y : Neo4jNode
        {
            if (NodeSrc == null || NodeDst == null)
                throw new ArgumentNullException();

            var edgeInstance = Activator.CreateInstance<E>();
            var relationshipType = edgeInstance.Name;

            string srcNodeType = NodeSrc.GetStringType();
            string dstNodeType = NodeDst.GetStringType();

            var query = $@"
                MATCH (a:{srcNodeType} {{ext_id: $srcId}})-[r:{relationshipType}]->(b:{dstNodeType} {{ext_id: $dstId}})
                RETURN 
                    elementId(r) as elementId,
                    type(r) as relationshipType,
                    properties(r) as properties,
                    a.ext_id as srcId,
                    b.ext_id as dstId
                ORDER BY r.date DESC";
            
            var parameters = new Dictionary<string, object>
            {
                ["srcId"] = NodeSrc.Id,
                ["dstId"] = NodeDst.Id
            };
            
            await using var session = _context.AsyncSession();
            
            try
            {
                var edges = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var results = new List<E>();
                    
                    while (await cursor.FetchAsync())
                    {
                        var edge = Activator.CreateInstance<E>();
                        var properties = cursor.Current["properties"].As<Dictionary<string, object>>();
                        
                        edge.Name = cursor.Current["relationshipType"].As<string>();
                        
                        if (properties.ContainsKey("date") && properties["date"] is DateTime date)
                        {
                            edge.Date = date;
                        }
                        
                        if (edge is PurchasedEdge purchasedEdge && properties.ContainsKey("rating"))
                        {
                            purchasedEdge.Rating = Convert.ToInt32(properties["rating"]);
                        }
                        else if (edge is QuantityEdge quantityEdge && properties.ContainsKey("quantity"))
                        {
                            quantityEdge.Quantity = Convert.ToInt32(properties["quantity"]);
                        }
                        else if (edge is PurchasedEdge && properties.ContainsKey("edgeCode"))
                        {
                            var edgeCode = Convert.ToInt32(properties["edgeCode"]);
                        }
                        
                        results.Add(edge);
                    }
                    
                    return results;
                });
                
                return edges;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting edges: {ex.Message}");
                Console.WriteLine($"Query: {query}");
                throw;
            }
        }
        public async Task<bool> DeleteEdgeAsync<E, T, Y>(E Edge, T NodeSrc, Y NodeDst) where E:Neo4jEdge where T: Neo4jNode where Y: Neo4jNode
        {
            if (NodeSrc == null || NodeDst == null)
                throw new ArgumentNullException();
            
            string srcNodeType = NodeSrc.GetStringType();
            string dstNodeType = NodeDst.GetStringType();

            string relationshipType;
            if(Edge == null)
                relationshipType = "[r]";
            else
                relationshipType = $"[r: {Edge.Name}]";
            
            var query = $@"
                MATCH (a:{srcNodeType} {{ext_id: $srcId}})-{relationshipType}->(b:{dstNodeType} {{ext_id: $dstId}})
                DELETE r
                RETURN COUNT(r) as deletedCount";
            
            var parameters = new Dictionary<string, object>
            {
                ["srcId"] = NodeSrc.Id,
                ["dstId"] = NodeDst.Id
            };
            
            await using var session = _context.AsyncSession();
            
            try
            {
                var result = await session.ExecuteWriteAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var record = await cursor.SingleAsync();
                    var deletedCount = record["deletedCount"].As<int>();
                    
                    Console.WriteLine($"Deleted {deletedCount} edge(s) between {NodeSrc.Id} and {NodeDst.Id}");
                    return deletedCount > 0;
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting edge: {ex.Message}");
                Console.WriteLine($"Query: {query}");
                throw;
            }
        }
        public async Task<bool> ClearDatabaseAsync()
        {
            var query = @"
                MATCH (n)
                DETACH DELETE n
                RETURN COUNT(n) as deletedCount";
            
            await using var session = _context.AsyncSession();
            
            try
            {
                var result = await session.ExecuteWriteAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query);
                    var record = await cursor.SingleAsync();
                    var deletedCount = record["deletedCount"].As<int>();
                    
                    Console.WriteLine($"Database cleared: {deletedCount} nodes deleted");
                    return true;
                });
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing database: {ex.Message}");
                throw;
            }
        }
        public async Task<List<T>> GetNodesByTypeAsync<T>(string nodetype) where T : Neo4jNode
        {
            var query = $@"
            MATCH (n:{nodetype})
            RETURN 
                n.ext_id as ext_id,
                n.type as type,
                n.name as name,
                properties(n) as properties
            ORDER BY n.ext_id";

            await using var session = _context.AsyncSession();
            try
            {
                var nodes = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query);
                    var results = new List<T>();
                    
                    while (await cursor.FetchAsync())
                    {
                        var node = Activator.CreateInstance<T>();
                        var properties = cursor.Current["properties"].As<Dictionary<string, object>>();
                        
                        node.Id = cursor.Current["ext_id"].As<string>();
                        
                        if (properties.ContainsKey("name"))
                            node.Name = properties["name"]?.ToString();
                        
                        if (node is ProductNode productNode)
                        {
                            if (properties.ContainsKey("tags") && properties["tags"] is List<object> tagList)
                            {
                                productNode.Tags = tagList.Select(t => t.ToString()).ToList();
                            }
                            if (properties.ContainsKey("createdAt") && properties["createdAt"] is DateTime createdAt)
                            {
                                productNode.CreatedAt = createdAt;
                            }
                        }
                        else if (node is StoreNode storeNode)
                        {
                            if (properties.ContainsKey("address"))
                                storeNode.Address = properties["address"]?.ToString() ?? string.Empty;
                            if (properties.ContainsKey("capacity") && properties["capacity"] is long capacity)
                            {
                                storeNode.Capacity = (int)capacity;
                            }
                        }
                        
                        results.Add(node);
                    }
                    
                    return results;
                });
                
                return nodes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting nodes of type {typeof(T).Name}: {ex.Message}");
                throw;
            }

        }
        public async Task<QueryResult> ExecuteCypherAsync(string query, object parameters = null)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        
        public async Task SeedTestDataAsync(
            int userCount = 30, int productCount = 50, int storeCount = 10,
            double viewedProb = 0.35, double likedProb = 0.35,
            double purchasedProb = 0.35, double boughtTogetherProb = 0.45,
            double visitedProb = 0.35, double quantityProb = 0.35,
            double shownProb = 0.35
            )
        {
            await ClearDatabaseAsync();
            var random = new Random();
            var list_users = new List<UserNode>();
            var list_products = new List<ProductNode>();
            var list_stores = new List<StoreNode>();
 
            for (int i = 1; i <= userCount; i++)
            {
                var user = new UserNode
                {
                    Id = $"user_{i:D3}",
                    Name = $"User {i}"
                };
                await CreateNodeAsync(user);
                list_users.Add(user);
            }
            
            var tags = new[] { "electronics", "clothing", "books", "food", "sports", "toys", "beauty", "home" };
            for (int i = 1; i <= productCount; i++)
            {
                var product = new ProductNode
                {
                    Id = $"product_{i:D3}",
                    Name = $"Product {i}",
                    Tags = new List<string> { tags[random.Next(tags.Length)], tags[random.Next(tags.Length)] }.Distinct().ToList(),
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365))
                };
                await CreateNodeAsync(product);
                list_products.Add(product);
            }
            
            var cities = new[] { "Moscow", "Saint Petersburg", "Kazan", "Novosibirsk", "Yekaterinburg" };
            for (int i = 1; i <= storeCount; i++)
            {
                var store = new StoreNode
                {
                    Id = $"store_{i:D3}",
                    Name = $"Store {i}",
                    Address = $"{cities[random.Next(cities.Length)]}, Street {random.Next(1, 100)}",
                    Capacity = random.Next(50, 500)
                };
                await CreateNodeAsync(store);
                list_stores.Add(store);
            }
            
            int totalRelationships = 0;
            
            foreach (var user in list_users)
            {
                foreach (var product in list_products)
                {
                    if (random.NextDouble() < viewedProb)
                    {
                        var viewedEdge = new ViewedEdge();
                        await CreateEdgeAsync(user, product, viewedEdge);
                        totalRelationships++;
                    }
                    
                    if (random.NextDouble() < likedProb)
                    {
                        var likedEdge = new LikedEdge();
                        await CreateEdgeAsync(user, product, likedEdge);
                        totalRelationships++;
                    }
                    
                    if (random.NextDouble() < purchasedProb)
                    {
                        var purchasedEdge = new PurchasedEdge
                        {
                            Rating = random.Next(1, 6)
                        };
                        await CreateEdgeAsync(user, product, purchasedEdge);
                        totalRelationships++;
                    }
                    
                    if (random.NextDouble() < shownProb)
                    {
                        var shownEdge = new ShownEdge();
                        await CreateEdgeAsync(product, user, shownEdge);
                        totalRelationships++;
                    }
                }
            }
            
            for (int i = 0; i < list_products.Count; i++)
            {
                for (int j = i + 1; j < list_products.Count; j++)
                {
                    if (random.NextDouble() < boughtTogetherProb)
                    {
                        var boughtTogetherEdge = new BoughtTogetherEdge();
                        await CreateEdgeAsync(list_products[i], list_products[j], boughtTogetherEdge);
                        totalRelationships++;
                    }
                }
            }
            
            foreach (var user in list_users)
            {
                foreach (var store in list_stores)
                {
                    if (random.NextDouble() < visitedProb)
                    {
                        var visitedEdge = new VisitedEdge();
                        await CreateEdgeAsync(user, store, visitedEdge);
                        totalRelationships++;
                    }
                }
            }
            
            foreach (var store in list_stores)
            {
                foreach (var product in list_products)
                {
                    if (random.NextDouble() < quantityProb)
                    {
                        var quantityEdge = new QuantityEdge
                        {
                            Quantity = random.Next(0, 100)
                        };
                        await CreateEdgeAsync(store, product, quantityEdge);
                        totalRelationships++;
                    }
                }
            }
        }
        public async Task<List<ProductNode>> GetViewedProductsByUserAsync(UserNode user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user.Id));

            var query = @"
                MATCH (u:User {ext_id: $userId})-[r:VIEWED]->(p:Product)
                RETURN 
                    p.ext_id as ext_id,
                    p.name as name,
                    p.type as type,
                    p.tags as tags,
                    p.createdAt as createdAt,
                    r.date as viewDate,
                    r.type as edgeType
                ORDER BY r.date DESC
                LIMIT 50";

            var parameters = new Dictionary<string, object>
            {
                ["userId"] = user.Id
            };

            await using var session = _context.AsyncSession();

            try
            {
                var products = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var results = new List<ProductNode>();

                    while (await cursor.FetchAsync())
                    {
                        var product = new ProductNode
                        {
                            Id = cursor.Current["ext_id"].As<string>(),
                            Name = cursor.Current["name"]?.As<string>() ?? string.Empty
                        };

                        if (cursor.Current["tags"] is List<object> tagList)
                        {
                            product.Tags = tagList.Select(t => t.ToString()).ToList();
                        }

                        if (cursor.Current["createdAt"] is DateTime createdAt)
                        {
                            product.CreatedAt = createdAt;
                        }

                        results.Add(product);
                    }

                    return results;
                });

                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting viewed products for user {user.Id}: {ex.Message}");
                throw;
            }
        }
        public async Task<List<UserNode>> GetUsersWhoLikedProductAsync(ProductNode product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product.Id));

            var query = @"
                MATCH (u:User)-[r:LIKED]->(p:Product {ext_id: $productId})
                RETURN 
                    u.ext_id as ext_id,
                    u.name as name,
                    u.type as type,
                    r.date as likeDate
                ORDER BY r.date DESC";

            var parameters = new Dictionary<string, object>
            {
                ["productId"] = product.Id
            };

            await using var session = _context.AsyncSession();

            try
            {
                var users = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var results = new List<UserNode>();

                    while (await cursor.FetchAsync())
                    {
                        var user = new UserNode
                        {
                            Id = cursor.Current["ext_id"].As<string>(),
                            Name = cursor.Current["name"]?.As<string>() ?? string.Empty
                        };

                        results.Add(user);
                    }

                    return results;
                });

                return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting users who liked product {product.Id}: {ex.Message}");
                throw;
            }
        }
        public async Task<List<ProductNode>> GetRecommendedProductsbyUserAsync(UserNode user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user.Id));

            var query = @"
                MATCH (u:User {ext_id: $userId})-[r1:VIEWED|LIKED]->(p:Product)
                
                MATCH (p)<-[r2:VIEWED|LIKED]-(other:User)
                WHERE other.ext_id <> $userId
                
                MATCH (other)-[r3:VIEWED|LIKED]->(rec:Product)
                WHERE NOT EXISTS((u)-[:VIEWED|LIKED]->(rec))
                
                RETURN 
                    rec.ext_id as ext_id,
                    rec.name as name,
                    rec.tags as tags,
                    rec.createdAt as createdAt,
                    COUNT(DISTINCT other) as recommendedBy,
                    COUNT(DISTINCT r3) as interactionCount
                ORDER BY recommendedBy DESC, interactionCount DESC
                LIMIT 20";

            var parameters = new Dictionary<string, object>
            {
                ["userId"] = user.Id
            };

            await using var session = _context.AsyncSession();

            try
            {
                var products = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var results = new List<ProductNode>();

                    while (await cursor.FetchAsync())
                    {
                        var product = new ProductNode
                        {
                            Id = cursor.Current["ext_id"].As<string>(),
                            Name = cursor.Current["name"]?.As<string>() ?? string.Empty
                        };

                        if (cursor.Current["tags"] is List<object> tagList)
                        {
                            product.Tags = tagList.Select(t => t.ToString()).ToList();
                        }

                        if (cursor.Current["createdAt"] is DateTime createdAt)
                        {
                            product.CreatedAt = createdAt;
                        }

                        results.Add(product);
                    }

                    return results;
                });

                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting recommendations for user {user.Id}: {ex.Message}");
                throw;
            }
        }
        public async Task<List<ProductNode>> GetRecommendedProductsbyProductAsync(ProductNode Product)
        {
            if (Product == null)
                throw new ArgumentNullException(nameof(Product.Id));

            var query = @"                
                MATCH (product:Product {ext_id: $productId})-[r:BOUGHT_TOGETHER]-(rec:Product)
                
                RETURN 
                    rec.ext_id as ext_id,
                    rec.name as name,
                    rec.tags as tags,
                    rec.createdAt as createdAt,
                    COUNT(r) as connectionStrength
                ORDER BY connectionStrength DESC
                LIMIT 20";

            var parameters = new Dictionary<string, object>
            {
                ["productId"] = Product.Id
            };

            await using var session = _context.AsyncSession();

            try
            {
                var products = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var results = new List<ProductNode>();

                    while (await cursor.FetchAsync())
                    {
                        var product = new ProductNode
                        {
                            Id = cursor.Current["ext_id"].As<string>(),
                            Name = cursor.Current["name"]?.As<string>() ?? string.Empty
                        };

                        if (cursor.Current["tags"] is List<object> tagList)
                        {
                            product.Tags = tagList.Select(t => t.ToString()).ToList();
                        }

                        if (cursor.Current["createdAt"] is DateTime createdAt)
                        {
                            product.CreatedAt = createdAt;
                        }

                        results.Add(product);
                    }

                    return results;
                });

                return products;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting recommendations for user {Product.Id}: {ex.Message}");
                throw;
            }
        }
        public async Task<List<UserNode>> GetConnectedUsersAsync(UserNode user, int minDepth = 2, int maxDepth = 4)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var query = @$"
                MATCH path = (u1:User {{ext_id: $userId}})-[*{minDepth}..{maxDepth}]-(u2:User)
                WHERE u1.ext_id <> u2.ext_id
                AND ALL(rel IN relationships(path) WHERE type(rel) IN ['VIEWED', 'LIKED', 'PURCHASED'])
                RETURN 
                    u2.ext_id as ext_id,
                    u2.name as name,
                    u2.type as type,
                    length(path) as depth
                ORDER BY depth
                LIMIT 50";

            var parameters = new Dictionary<string, object>
            {
                ["userId"] = user.Id
            };

            await using var session = _context.AsyncSession();

            try
            {
                var users = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var results = new List<UserNode>();
                    var seenIds = new HashSet<string>();

                    while (await cursor.FetchAsync())
                    {
                        var userId = cursor.Current["ext_id"].As<string>();
                        
                        if (!seenIds.Contains(userId))
                        {
                            seenIds.Add(userId);
                            
                            var connectedUser = new UserNode
                            {
                                Id = userId,
                                Name = cursor.Current["name"]?.As<string>() ?? string.Empty
                            };
                            
                            results.Add(connectedUser); 
                        }
                    }

                    return results;
                });

                return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting connected users for {user.Id}: {ex.Message}");
                throw;
            }
        }
        public async Task<List<Dictionary<string, object>>> FindPathBetweenUsersAsync(
            UserNode user1, UserNode user2, int maxDepth = 5)
        {
            if (user1 == null || user2 == null)
                throw new ArgumentNullException();

            var query = @$"
                MATCH path = shortestPath(
                    (u1:User {{ext_id: $userId1}})-[*1..{maxDepth}]-(u2:User {{ext_id: $userId2}})
                )
                RETURN 
                    length(path) as pathLength,
                    [node IN nodes(path) | 
                        {{
                            id: node.ext_id,
                            type: labels(node)[0],
                            name: node.name
                        }}
                    ] as nodes,
                    [rel IN relationships(path) | 
                        {{
                            type: type(rel),
                            properties: properties(rel)
                        }}
                    ] as relationships,
                    path";

            var parameters = new Dictionary<string, object>
            {
                ["userId1"] = user1.Id,
                ["userId2"] = user2.Id,
            };

            await using var session = _context.AsyncSession();

            try
            {
                var paths = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var results = new List<Dictionary<string, object>>();

                    while (await cursor.FetchAsync())
                    {
                        var result = new Dictionary<string, object>
                        {
                            ["pathLength"] = cursor.Current["pathLength"].As<int>(),
                            ["nodes"] = cursor.Current["nodes"].As<List<object>>(),
                            ["relationships"] = cursor.Current["relationships"].As<List<object>>()
                        };
                        results.Add(result);
                    }

                    return results;
                });

                return paths;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding path between {user1.Id} and {user2.Id}: {ex.Message}");
                throw;
            }
        }
        public async Task<List<Dictionary<string, object>>> GetUserConnectionsWithDepthAsync(
            UserNode user, int maxDepth = 4, int minDepth = 1)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var query = @$"
                MATCH path = (u:User {{ext_id: $userId}})-[*{minDepth}..{maxDepth}]-(connected:User)
                WHERE u <> connected
                RETURN 
                    connected.ext_id as userId,
                    connected.name as userName,
                    length(path) as depth,
                    [rel IN relationships(path) | type(rel)] as connectionTypes,
                    [node IN nodes(path)[1..-1] | node.ext_id] as intermediateNodes,
                    length(path) as pathLength
                ORDER BY depth, connected.ext_id
                LIMIT 100";

            var parameters = new Dictionary<string, object>
            {
                ["userId"] = user.Id
            };

            await using var session = _context.AsyncSession();

            try
            {
                var connections = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var results = new List<Dictionary<string, object>>();

                    while (await cursor.FetchAsync())
                    {
                        var connection = new Dictionary<string, object>
                        {
                            ["userId"] = cursor.Current["userId"].As<string>(),
                            ["userName"] = cursor.Current["userName"]?.As<string>() ?? string.Empty,
                            ["depth"] = cursor.Current["depth"].As<int>(),
                            ["connectionTypes"] = cursor.Current["connectionTypes"].As<List<object>>(),
                            ["intermediateNodes"] = cursor.Current["intermediateNodes"].As<List<object>>()
                        };
                        results.Add(connection);
                    }

                    return results;
                });

                return connections;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting connections with depth for {user.Id}: {ex.Message}");
                throw;
            }
        }
        
        public async Task<List<UserCommonResult>>  GetUsersWithCommonEdgeAsync(
            UserNode user, Neo4jEdge edge, int limit = 20)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            string targetNodeType = edge.TypeNodeDst == TypeNode.PRODUCT ? "Product" : "Store";
            var query = @$"
                MATCH (u:User {{ext_id: $userId}})-[:{edge.Name}]->(p:{targetNodeType})
                
                MATCH (p)<-[:{edge.Name}]-(other:User)
                WHERE other.ext_id <> $userId
                
                WITH other, COUNT(DISTINCT p) as commonCount
                
                RETURN 
                    other.ext_id as ext_id,
                    other.name as name,
                    other.type as type,
                    commonCount
                ORDER BY commonCount DESC
                LIMIT $limit";

            var parameters = new Dictionary<string, object>
            {
                ["userId"] = user.Id,
                ["limit"] = limit
            };

            await using var session = _context.AsyncSession();

            try
            {
                var result = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var results = new List<UserCommonResult>();

                    while (await cursor.FetchAsync())
                    {
                        var otherUser = new UserNode
                        {
                            Id = cursor.Current["ext_id"].As<string>(),
                            Name = cursor.Current["name"]?.As<string>() ?? string.Empty
                        };
                        
                        var commonCount = cursor.Current["commonCount"].As<int>();
                        
                        results.Add(new UserCommonResult
                        {
                            User = otherUser,
                            CommonCount = commonCount
                        });
                    }

                    return results;
                });

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting users with common edge for {user.Id}: {ex.Message}");
                throw;
            }
        }
        public async Task<List<ProductCountResult>> GetTopProductsAsync(string typeEdge, int limit = 10)
        {
            var query = @$"
                MATCH (p:Product)<-[r:{typeEdge}]-(:User)
                RETURN 
                    p.ext_id as ext_id,
                    p.name as name,
                    p.tags as tags,
                    p.createdAt as createdAt,
                    COUNT(r) as Count
                ORDER BY Count DESC
                LIMIT $limit";

            var parameters = new Dictionary<string, object> { ["limit"] = limit };

            await using var session = _context.AsyncSession();

            try
            {
                var result = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var results = new List<ProductCountResult>();

                    while (await cursor.FetchAsync())
                    {
                        var product = new ProductNode
                        {
                            Id = cursor.Current["ext_id"].As<string>(),
                            Name = cursor.Current["name"]?.As<string>() ?? string.Empty
                        };

                        if (cursor.Current["tags"] is List<object> tagList)
                        {
                            product.Tags = tagList.Select(t => t.ToString()).ToList();
                        }

                        if (cursor.Current["createdAt"] is DateTime createdAt)
                        {
                            product.CreatedAt = createdAt;
                        }

                        var Count = cursor.Current["Count"].As<int>();
                        results.Add(new ProductCountResult
                        {
                            Product = product,
                            count = Count
                        });
                    }

                    return results;
                });

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting top viewed products: {ex.Message}");
                throw;
            }
        }
        public async Task<List<ProductTagStatistik>> GetTagStatisticsAsync()
        {
            var query = @"
                MATCH (p:Product)
                UNWIND p.tags as tag
                OPTIONAL MATCH (p)<-[r:VIEWED]-()
                RETURN 
                    tag,
                    COUNT(DISTINCT p) as productCount,
                    COUNT(r) as totalViews
                ORDER BY productCount DESC, totalViews DESC";

            await using var session = _context.AsyncSession();

            try
            {
                var result = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query);
                    var results = new List<ProductTagStatistik>();

                    while (await cursor.FetchAsync())
                    {
                        var tag = cursor.Current["tag"].As<string>();
                        var productCount = cursor.Current["productCount"].As<int>();
                        var totalViews = cursor.Current["totalViews"].As<int>();
                        
                        results.Add(new ProductTagStatistik
                        {
                            Tag = tag,
                            ProductCount = productCount,
                            TotalViews = totalViews
                        });
                    }

                    return results;
                });

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting tag statistics: {ex.Message}");
                throw;
            }
        }

        public async Task<List<UserPurchaseSummary>> GetTopUsersByViewedAndPurchasedAsync(int limit = 10)
        {
            var query = @"
                MATCH (u:User)-[:VIEWED]->(p:Product)<-[:PURCHASED]-(u)
                WITH u, p, COUNT(*) as interaction_count
                RETURN 
                    u.ext_id as user_id,
                    u.name as user_name,
                    COLLECT(p {
                        .ext_id, 
                        .name, 
                        .tags, 
                        .createdAt
                    }) as products,
                    SUM(interaction_count) as total_purchases
                ORDER BY total_purchases DESC
                LIMIT $limit";

            var parameters = new Dictionary<string, object>
            {
                ["limit"] = limit
            };

            await using var session = _context.AsyncSession();

            try
            {
                var result = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var results = new List<UserPurchaseSummary>();

                    while (await cursor.FetchAsync())
                    {
                        var user = new UserNode
                        {
                            Id = cursor.Current["user_id"].As<string>(),
                            Name = cursor.Current["user_name"]?.As<string>() ?? string.Empty
                        };

                        var totalPurchases = cursor.Current["total_purchases"].As<int>();
                        
                        // Получаем продукты
                        var products = new List<ProductNode>();
                        var productsData = cursor.Current["products"].As<List<Dictionary<string, object>>>();
                        
                        foreach (var productData in productsData)
                        {
                            var product = new ProductNode
                            {
                                Id = productData["ext_id"].ToString(),
                                Name = productData["name"]?.ToString() ?? string.Empty
                            };

                            if (productData.ContainsKey("tags") && productData["tags"] is List<object> tagList)
                                product.Tags = tagList.Select(t => t.ToString()).ToList();

                            if (productData.ContainsKey("createdAt") && productData["createdAt"] is DateTime createdAt)
                                product.CreatedAt = createdAt;

                            products.Add(product);
                        }

                        results.Add(new UserPurchaseSummary
                        {
                            User = user,
                            Products = products,
                            TotalPurchases = totalPurchases,
                            ProductIds = products.Select(p => p.Id).ToList()
                        });
                    }

                    return results;
                });

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting top users by viewed and purchased: {ex.Message}");
                throw;
            }
        }
        /*
        MATCH (u:User)-[:VIEWED]->(p:Product)<-[:PURCHASED]-(u)
        WITH u, p, COUNT(*) as interaction_count
        RETURN u, COLLECT(p) as products, SUM(interaction_count) as total_purchases
        ORDER BY total_purchases DESC
        */
    }
}
