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
        Task<bool> CreateNodeAsync<T>(T node) where T : Neo4jNode;
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
                MATCH (n {ext_id: $ext_id})
                RETURN properties(n) as properties";
            var parameters = new Dictionary<string, object>
            {
                ["ext_id"] = nodeId
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
        
        public async Task<TRel> CreateEdgeAsync<TRel, T, Y>(T nodeSrc, Y nodeDst, TRel edge) where TRel : Neo4jEdge where T : Neo4jNode where Y : Neo4jNode
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