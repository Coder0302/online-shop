using System.Text.Json.Serialization;

namespace project.Models.Neo4jModels.Responses
{
    /// <summary>
    /// Базовый ответ для всех операций с Neo4j
    /// </summary>
    public class Neo4jResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;
        
        [JsonPropertyName("message")]
        public string Message { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Ответ на запрос с результатами
    /// </summary>
    public class QueryResult : Neo4jResponse
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }
        
        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        [JsonPropertyName("records")]
        public List<Dictionary<string, object>> Records { get; set; } = new();
        
        [JsonPropertyName("summary")]
        public QuerySummary Summary { get; set; } = new();
    }

    /// <summary>
    /// Сводка по выполненному запросу
    /// </summary>
    public class QuerySummary
    {
        [JsonPropertyName("nodesCreated")]
        public int NodesCreated { get; set; }
        
        [JsonPropertyName("nodesDeleted")]
        public int NodesDeleted { get; set; }
        
        [JsonPropertyName("relationshipsCreated")]
        public int RelationshipsCreated { get; set; }
        
        [JsonPropertyName("relationshipsDeleted")]
        public int RelationshipsDeleted { get; set; }
        
        [JsonPropertyName("propertiesSet")]
        public int PropertiesSet { get; set; }
        
        [JsonPropertyName("labelsAdded")]
        public int LabelsAdded { get; set; }
        
        [JsonPropertyName("labelsRemoved")]
        public int LabelsRemoved { get; set; }
        
        [JsonPropertyName("indexesAdded")]
        public int IndexesAdded { get; set; }
        
        [JsonPropertyName("indexesRemoved")]
        public int IndexesRemoved { get; set; }
        
        [JsonPropertyName("constraintsAdded")]
        public int ConstraintsAdded { get; set; }
        
        [JsonPropertyName("constraintsRemoved")]
        public int ConstraintsRemoved { get; set; }
        
        [JsonPropertyName("containsUpdates")]
        public bool ContainsUpdates { get; set; }
        
        [JsonPropertyName("resultAvailableAfterMs")]
        public long ResultAvailableAfterMs { get; set; }
        
        [JsonPropertyName("resultConsumedAfterMs")]
        public long ResultConsumedAfterMs { get; set; }
    }

    /// <summary>
    /// Ответ с результатом поиска узла
    /// </summary>
    public class NodeResponse : Neo4jResponse
    {
        [JsonPropertyName("node")]
        public NodeResult Node { get; set; }
    }

    /// <summary>
    /// Результат узла с его свойствами
    /// </summary>
    public class NodeResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();
        
        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; } = new();
        
        [JsonPropertyName("degree")]
        public NodeDegree Degree { get; set; } = new();
    }

    /// <summary>
    /// Степень узла (количество связей)
    /// </summary>
    public class NodeDegree
    {
        [JsonPropertyName("incoming")]
        public int Incoming { get; set; }
        
        [JsonPropertyName("outgoing")]
        public int Outgoing { get; set; }
        
        [JsonPropertyName("total")]
        public int Total => Incoming + Outgoing;
    }

    /// <summary>
    /// Ответ с результатом поиска связи
    /// </summary>
    public class RelationshipResponse : Neo4jResponse
    {
        [JsonPropertyName("relationship")]
        public RelationshipResult Relationship { get; set; }
    }

    /// <summary>
    /// Результат связи с её свойствами
    /// </summary>
    public class RelationshipResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("fromNodeId")]
        public string FromNodeId { get; set; }
        
        [JsonPropertyName("toNodeId")]
        public string ToNodeId { get; set; }
        
        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; } = new();
        
        [JsonPropertyName("fromNode")]
        public NodeResult FromNode { get; set; }
        
        [JsonPropertyName("toNode")]
        public NodeResult ToNode { get; set; }
    }

    /// <summary>
    /// Ответ со списком узлов
    /// </summary>
    public class NodesListResponse : Neo4jResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        
        [JsonPropertyName("nodes")]
        public List<NodeResult> Nodes { get; set; } = new();
    }

    /// <summary>
    /// Ответ со списком связей
    /// </summary>
    public class RelationshipsListResponse : Neo4jResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        
        [JsonPropertyName("relationships")]
        public List<RelationshipResult> Relationships { get; set; } = new();
    }

    /// <summary>
    /// Ответ с результатом пути (path)
    /// </summary>
    public class PathResponse : Neo4jResponse
    {
        [JsonPropertyName("paths")]
        public List<PathResult> Paths { get; set; } = new();
        
        [JsonPropertyName("totalLength")]
        public int TotalLength { get; set; }
    }

    /// <summary>
    /// Результат пути в графе
    /// </summary>
    public class PathResult
    {
        [JsonPropertyName("nodes")]
        public List<NodeResult> Nodes { get; set; } = new();
        
        [JsonPropertyName("relationships")]
        public List<RelationshipResult> Relationships { get; set; } = new();
        
        [JsonPropertyName("length")]
        public int Length { get; set; }
        
        [JsonPropertyName("cost")]
        public double? Cost { get; set; }
        
        [JsonPropertyName("pathPattern")]
        public string PathPattern { get; set; }
    }

    /// <summary>
    /// Ответ с агрегированными результатами
    /// </summary>
    public class AggregationResponse : Neo4jResponse
    {
        [JsonPropertyName("aggregates")]
        public Dictionary<string, object> Aggregates { get; set; } = new();
        
        [JsonPropertyName("groupedResults")]
        public List<Dictionary<string, object>> GroupedResults { get; set; } = new();
        
        [JsonPropertyName("groupsCount")]
        public int GroupsCount => GroupedResults?.Count ?? 0;
    }

    /// <summary>
    /// Ответ для статистических запросов
    /// </summary>
    public class StatisticsResponse : Neo4jResponse
    {
        [JsonPropertyName("databaseStats")]
        public DatabaseStatistics DatabaseStats { get; set; } = new();
        
        [JsonPropertyName("queryStats")]
        public QueryStatistics QueryStats { get; set; } = new();
    }

    /// <summary>
    /// Статистика базы данных
    /// </summary>
    public class DatabaseStatistics
    {
        [JsonPropertyName("totalNodes")]
        public int TotalNodes { get; set; }
        
        [JsonPropertyName("totalRelationships")]
        public int TotalRelationships { get; set; }
        
        [JsonPropertyName("nodesByType")]
        public Dictionary<string, int> NodesByType { get; set; } = new();
        
        [JsonPropertyName("relationshipsByType")]
        public Dictionary<string, int> RelationshipsByType { get; set; } = new();
        
        [JsonPropertyName("averageNodeDegree")]
        public double AverageNodeDegree { get; set; }
        
        [JsonPropertyName("density")]
        public double Density { get; set; }
    }

    /// <summary>
    /// Статистика запроса
    /// </summary>
    public class QueryStatistics
    {
        [JsonPropertyName("planningTimeMs")]
        public long PlanningTimeMs { get; set; }
        
        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }
        
        [JsonPropertyName("pageCacheHits")]
        public long PageCacheHits { get; set; }
        
        [JsonPropertyName("pageCacheMisses")]
        public long PageCacheMisses { get; set; }
        
        [JsonPropertyName("pageCacheHitRatio")]
        public double PageCacheHitRatio => PageCacheHits + PageCacheMisses > 0 
            ? (double)PageCacheHits / (PageCacheHits + PageCacheMisses) 
            : 0;
    }

    /// <summary>
    /// Ответ для операций с индексами
    /// </summary>
    public class IndexResponse : Neo4jResponse
    {
        [JsonPropertyName("indexes")]
        public List<IndexInfo> Indexes { get; set; } = new();
    }

    /// <summary>
    /// Информация об индексе
    /// </summary>
    public class IndexInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("state")]
        public string State { get; set; }
        
        [JsonPropertyName("populationPercent")]
        public double PopulationPercent { get; set; }
        
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();
        
        [JsonPropertyName("properties")]
        public List<string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Ответ для рекомендательных систем
    /// </summary>
    public class RecommendationResponse : Neo4jResponse
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }
        
        [JsonPropertyName("recommendations")]
        public List<Recommendation> Recommendations { get; set; } = new();
        
        [JsonPropertyName("algorithm")]
        public string Algorithm { get; set; }
        
        [JsonPropertyName("confidenceThreshold")]
        public double ConfidenceThreshold { get; set; }
    }

    /// <summary>
    /// Рекомендация для пользователя
    /// </summary>
    public class Recommendation
    {
        [JsonPropertyName("itemId")]
        public string ItemId { get; set; }
        
        [JsonPropertyName("itemType")]
        public string ItemType { get; set; }
        
        [JsonPropertyName("itemName")]
        public string ItemName { get; set; }
        
        [JsonPropertyName("score")]
        public double Score { get; set; }
        
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
        
        [JsonPropertyName("reason")]
        public string Reason { get; set; }
        
        [JsonPropertyName("similarUsersCount")]
        public int SimilarUsersCount { get; set; }
    }

    /// <summary>
    /// Ответ для анализа сообществ
    /// </summary>
    public class CommunityAnalysisResponse : Neo4jResponse
    {
        [JsonPropertyName("communities")]
        public List<Community> Communities { get; set; } = new();
        
        [JsonPropertyName("modularity")]
        public double Modularity { get; set; }
        
        [JsonPropertyName("averageCommunitySize")]
        public double AverageCommunitySize { get; set; }
    }

    /// <summary>
    /// Сообщество в графе
    /// </summary>
    public class Community
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("size")]
        public int Size { get; set; }
        
        [JsonPropertyName("nodes")]
        public List<NodeResult> Nodes { get; set; } = new();
        
        [JsonPropertyName("density")]
        public double Density { get; set; }
        
        [JsonPropertyName("centralNode")]
        public NodeResult CentralNode { get; set; }
    }

    /// <summary>
    /// Ответ для запросов с ошибкой
    /// </summary>
    public class ErrorResponse : Neo4jResponse
    {
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }
        
        [JsonPropertyName("errorDetails")]
        public string ErrorDetails { get; set; }
        
        [JsonPropertyName("stackTrace")]
        public string StackTrace { get; set; }
        
        [JsonPropertyName("suggestion")]
        public string Suggestion { get; set; }

        public ErrorResponse()
        {
            Success = false;
        }
        
        public ErrorResponse(string message, Exception ex = null)
        {
            Success = false;
            Message = message;
            
            if (ex != null)
            {
                ErrorDetails = ex.Message;
                ErrorCode = ex.GetType().Name;
                StackTrace = ex.StackTrace;
                
                // Предложения по исправлению распространенных ошибок
                if (ex.Message.Contains("ConstraintValidationFailed"))
                {
                    Suggestion = "Убедитесь, что значение удовлетворяет ограничениям уникальности";
                }
                else if (ex.Message.Contains("NodeNotFound"))
                {
                    Suggestion = "Проверьте существование узла с указанным ID";
                }
                else if (ex.Message.Contains("SyntaxError"))
                {
                    Suggestion = "Проверьте синтаксис Cypher-запроса";
                }
            }
        }
    }

    /// <summary>
    ///Ответ для пакетных операций
    /// </summary>
    public class BatchResponse : Neo4jResponse
    {
        [JsonPropertyName("operations")]
        public List<BatchOperationResult> Operations { get; set; } = new();
        
        [JsonPropertyName("successfulCount")]
        public int SuccessfulCount => Operations.Count(o => o.Success);
        
        [JsonPropertyName("failedCount")]
        public int FailedCount => Operations.Count(o => !o.Success);
    }

    /// <summary>
    /// Результат одной операции в пакете
    /// </summary>
    public class BatchOperationResult
    {
        [JsonPropertyName("operationId")]
        public string OperationId { get; set; }
        
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; }
        
        [JsonPropertyName("result")]
        public object Result { get; set; }
        
        [JsonPropertyName("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Ответ для операций импорта/экспорта
    /// </summary>
    public class ImportExportResponse : Neo4jResponse
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }
        
        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }
        
        [JsonPropertyName("format")]
        public string Format { get; set; }
        
        [JsonPropertyName("importStats")]
        public ImportStatistics ImportStats { get; set; } = new();
    }

    /// <summary>
    /// Статистика импорта
    /// </summary>
    public class ImportStatistics
    {
        [JsonPropertyName("nodesImported")]
        public int NodesImported { get; set; }
        
        [JsonPropertyName("relationshipsImported")]
        public int RelationshipsImported { get; set; }
        
        [JsonPropertyName("propertiesImported")]
        public int PropertiesImported { get; set; }
        
        [JsonPropertyName("durationMs")]
        public long DurationMs { get; set; }
    }
}