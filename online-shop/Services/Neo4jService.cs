using project.Models.Neo4jModels;
using Neo4j.Driver;
using project.Models.Neo4jModels.Responses;
using System.Data;
using DnsClient.Protocol;
using System.Text.Json;

namespace project.Services
{
    public interface INeo4jService
    {
        Task<int> GetTypeWithId(string nodeId);
        Task<T> CreateNodeAsync<T>(T node) where T : Neo4jNode;
        Task<Dictionary<string, object>> GetNodeAsync<T>(T node) where T : Neo4jNode;
        Task<bool> UpdateNodeAsync<T>(T node) where T : Neo4jNode;
        Task<bool> DeleteNodeAsync<T>(T node) where T : Neo4jNode;
        
        Task<TRel> CreateEdgeAsync<TRel, T, Y>(T srcNode, Y dstNode, TRel Edge) 
            where TRel : Neo4jEdge where T : Neo4jNode where Y : Neo4jNode;
        Task<List<TRel>> GetEdgesAsync<TRel>(string nodeId) where TRel : Neo4jEdge;
        Task<bool> DeleteEdgeAsync(string EdgeId);
        
        Task<List<T>> GetNodesByTypeAsync<T>() where T : Neo4jNode;
        Task<QueryResult> ExecuteCypherAsync(string query, object parameters = null);
        
        Task SeedTestDataAsync(int users = 30, int products = 50, int stores = 10);
        Task<List<ProductNode>> GetViewedProductsByUserAsync(string userId);
        Task<List<UserNode>> GetUsersWhoLikedProductAsync(string productId);
        Task<List<ProductNode>> GetRecommendedProductsAsync(string userId);
        Task<List<ProductNode>> GetBoughtTogetherProductsAsync(string productId);
        Task<Dictionary<string, int>> GetProductAvailabilityInStoresAsync(string productId);
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
                MATCH (n {id: $id})
                RETURN properties(n) as properties";
            var parameters = new Dictionary<string, object>
            {
                ["id"] = nodeId
            };
            await using var session = _context.AsyncSession();
            try
            {
                var rezult = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
            
                    if (await cursor.FetchAsync())
                    {
                        var properties = cursor.Current["properties"].As<Dictionary<string, object>>();
                        return properties;
                    }
                    return null;
                });
                if(rezult == null)
                    return -1;
                return Convert.ToInt32(rezult["type"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting node: {ex.Message}");
                throw;
            }
        }

        public async Task<T> CreateNodeAsync<T>(T node) where T : Neo4jNode
        {
            if(node == null)
                throw new ArgumentNullException(nameof(node));
            
            string typename = node.GetStringType();

            var properties = node.ToProperties();
            var query = $@"
                MERGE (n:{typename} {{id: $id}})
                SET n = $properties
                RETURN n.id as id, n.type as type, n";
            var parameters = new { id = node.Id, properties};
            await using var session = _context.AsyncSession();
            try
            {
                var result = await session.ExecuteWriteAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var record = await cursor.SingleAsync();

                    node.Id = record["id"].As<string>();
                    return node;
                });
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating node: {ex.Message}");
                throw;
            }
        }
        public async Task<Dictionary<string, object>> GetNodeAsync<T>(T node) where T : Neo4jNode
        {
            if(node == null)
                throw new ArgumentNullException(nameof(node));

            string typename = node.GetStringType();
            var query = $@"
                MATCH (n:{typename} {{id: $id}})
                RETURN n.id as id, elementId(n) as elementId, properties(n) as properties";
            var parameters = new Dictionary<string, object>
            {
                ["id"] = node.Id
            };
            await using var session = _context.AsyncSession();
            try
            {
                var result = await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
            
                    if (await cursor.FetchAsync())
                    {
                        var id = cursor.Current["id"].As<string>();
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
            var properties = GetNodeAsync(node);
            if(properties == null)
                return false;
            await CreateNodeAsync(node);
            return true;
        }
        public async Task<bool> DeleteNodeAsync<T>(T node) where T : Neo4jNode
        {
            if(node == null)
                throw new ArgumentNullException(nameof(node));
            string typename = node.GetStringType();
            var query = $@"
                MATCH (n:{typename} {{id: $id}})
                DETACH DELETE n
                RETURN COUNT(n) as deletedCount";

            var parameters = new Dictionary<string, object>
            {
                ["id"] = node.Id
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
        
        public async Task<TRel> CreateEdgeAsync<TRel, T, Y>(T nodeSrc, Y nodeDst, TRel edge) where TRel : Neo4jEdge where T : Neo4jNode where Y : Neo4jNode
        {
            Console.WriteLine($"start CreateEdgeAsync");
            if(nodeSrc == null || nodeDst == null)
                throw new ArgumentNullException(nameof(nodeSrc));
            if(edge == null)
                throw new ArgumentNullException(nameof(edge));
            if(nodeSrc.Type != edge.TypeNodeSrc || nodeDst.Type != edge.TypeNodeDst)
            {
                Console.WriteLine("invalid edge src dst");
                return null;
            }
            Console.WriteLine($"point 0");
            var properties = edge.ToProperties();
            string relationshipType = edge.Name;
            string srcNodeType = nodeSrc.GetStringType();
            string dstNodeType = nodeDst.GetStringType();
            var query = $@"
                MATCH (a:{srcNodeType} {{id: $srcId}})
                MATCH (b:{dstNodeType} {{id: $dstId}})
                CREATE (a)-[r:{relationshipType} {{type: $edgeTypeCode}}]->(b)
                SET r+= $properties
                RETURN r, elementId(r) as elementId";
            Console.WriteLine($"point 1");
            var parameters = new Dictionary<string, object>
            {
                ["srcId"] = nodeSrc.Id,
                ["dstId"] = nodeDst.Id,
                ["edgeTypeCode"] = (int)edge.Type,
                ["properties"] = properties
            };
            Console.WriteLine($"point 2");
            await using var session = _context.AsyncSession();
            try
            {
                var rezult = await session.ExecuteWriteAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(query, parameters);
                    var record = await cursor.SingleAsync();
                    
                    var relationshipElementId = record["elementId"].As<string>();
                    
                    return edge;
                });
                return rezult;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error creating edge: {ex.Message}");
                Console.WriteLine($"Query: {query}");
                throw;
            }
        }
        public async Task<List<TRel>> GetEdgesAsync<TRel>(string nodeId) where TRel : Neo4jEdge
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<bool> DeleteEdgeAsync(string EdgeId)
        {
            Console.WriteLine("TEST1728");
            return true;
        }
        
        public async Task<List<T>> GetNodesByTypeAsync<T>() where T : Neo4jNode
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<QueryResult> ExecuteCypherAsync(string query, object parameters = null)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        
        public async Task SeedTestDataAsync(int users = 30, int products = 50, int stores = 10)
        {
            Console.WriteLine("TEST1728");
        }
        public async Task<List<ProductNode>> GetViewedProductsByUserAsync(string userId)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<List<UserNode>> GetUsersWhoLikedProductAsync(string productId)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<List<ProductNode>> GetRecommendedProductsAsync(string userId)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<List<ProductNode>> GetBoughtTogetherProductsAsync(string productId)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
        public async Task<Dictionary<string, int>> GetProductAvailabilityInStoresAsync(string productId)
        {
            Console.WriteLine("TEST1728");
            return null;
        }
    }
}