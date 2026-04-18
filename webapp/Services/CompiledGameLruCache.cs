using System.Collections.Concurrent;

namespace webapp.Services;

public sealed class CompiledGameLruCache
{
    private class LruCacheItem(string gameId, string gamePath, long sizeBytes)
    {
        public string GameId { get; } = gameId;
        public string GamePath { get; } = gamePath;
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
        public long SizeBytes { get; set; } = sizeBytes;
    }

    private const int MaxCapacity = 5;
    private readonly ConcurrentDictionary<string, LinkedListNode<LruCacheItem>> _cache;
    private readonly LinkedList<LruCacheItem> _linkedList;
    private readonly Lock _lock = new();
    private readonly string _compiledGamesPath;
    private readonly ILogger<CompiledGameLruCache> _logger;

    public CompiledGameLruCache(ILogger<CompiledGameLruCache> logger, IConfiguration configuration)
    {
        _logger = logger;
        _compiledGamesPath = configuration.GetValue<string>("CompiledGamesPath") ?? 
            Path.Combine(Directory.GetCurrentDirectory(), "CompiledGames");
        _cache = new ConcurrentDictionary<string, LinkedListNode<LruCacheItem>>();
        _linkedList = new LinkedList<LruCacheItem>();
        
        // Initialize with existing compiled games
        InitializeFromExistingGames();
    }

    private void InitializeFromExistingGames()
    {
        try
        {
            if (!Directory.Exists(_compiledGamesPath))
                return;

            var gameDirectories = Directory.GetDirectories(_compiledGamesPath, "game_*");
            
            foreach (var gameDir in gameDirectories.Take(MaxCapacity))
            {
                var gameId = Path.GetFileName(gameDir);
                var sizeBytes = CalculateDirectorySize(gameDir);
                
                var cacheItem = new LruCacheItem(gameId, gameDir, sizeBytes);
                var node = new LinkedListNode<LruCacheItem>(cacheItem);
                
                lock (_lock)
                {
                    if (_linkedList.Count < MaxCapacity)
                    {
                        _linkedList.AddFirst(node);
                        _cache[gameId] = node;
                    }
                }
            }
            
            _logger.LogInformation("LRU cache initialized with {Count} games", _cache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing LRU cache from existing games");
        }
    }

    public bool TryGetGame(string gameId, out string? gamePath)
    {
        gamePath = null;
        
        lock (_lock)
        {
            if (_cache.TryGetValue(gameId, out var node))
            {
                // Update access time and move to front
                node.Value.LastAccessed = DateTime.UtcNow;
                _linkedList.Remove(node);
                _linkedList.AddFirst(node);
                
                gamePath = node.Value.GamePath;
                return true;
            }
        }
        
        // Check if game exists in file system but not in cache
        var fullPath = Path.Combine(_compiledGamesPath, gameId);
        if (Directory.Exists(fullPath))
        {
            AddGame(gameId, fullPath);
            gamePath = fullPath;
            return true;
        }
        
        return false;
    }

    public void AddGame(string gameId, string gamePath)
    {
        var sizeBytes = CalculateDirectorySize(gamePath);
        var cacheItem = new LruCacheItem(gameId, gamePath, sizeBytes);
        
        lock (_lock)
        {
            if (_cache.TryGetValue(gameId, out var existingNode))
            {
                // Update existing entry
                existingNode.Value.LastAccessed = DateTime.UtcNow;
                existingNode.Value.SizeBytes = sizeBytes;
                _linkedList.Remove(existingNode);
                _linkedList.AddFirst(existingNode);
                return;
            }

            var newNode = new LinkedListNode<LruCacheItem>(cacheItem);

            // Remove least recently used games if capacity exceeded
            while (_linkedList.Count >= MaxCapacity)
            {
                var lruNode = _linkedList.Last;
                if (lruNode != null)
                {
                    RemoveGameFromFileSystem(lruNode.Value.GameId, lruNode.Value.GamePath);
                    _cache.TryRemove(lruNode.Value.GameId, out _);
                    _linkedList.RemoveLast();
                }
            }

            // Add new game to cache
            _linkedList.AddFirst(newNode);
            _cache[gameId] = newNode;
        }
        
        _logger.LogInformation("Added game {GameId} to LRU cache. Current cache size: {Count}", gameId, _cache.Count);
    }

    public bool RemoveGame(string gameId)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(gameId, out var node))
            {
                RemoveGameFromFileSystem(gameId, node.Value.GamePath);
                _cache.TryRemove(gameId, out _);
                _linkedList.Remove(node);
                
                _logger.LogInformation("Removed game {GameId} from LRU cache", gameId);
                return true;
            }
        }
        
        return false;
    }

    private void RemoveGameFromFileSystem(string gameId, string gamePath)
    {
        try
        {
            if (Directory.Exists(gamePath))
            {
                Directory.Delete(gamePath, true);
                _logger.LogInformation("Removed game directory: {GamePath}", gamePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove game directory: {GamePath}", gamePath);
        }
    }

    public void CleanupOldGames(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var gamesToRemove = new List<string>();
        
        lock (_lock)
        {
            var currentNode = _linkedList.Last;
            while (currentNode != null)
            {
                if (currentNode.Value.LastAccessed < cutoffTime)
                {
                    gamesToRemove.Add(currentNode.Value.GameId);
                }
                currentNode = currentNode.Previous;
            }
        }

        foreach (var gameId in gamesToRemove)
        {
            RemoveGame(gameId);
        }
        
        _logger.LogInformation("Cleanup removed {Count} old games", gamesToRemove.Count);
    }

    public IEnumerable<string> GetCachedGameIds()
    {
        lock (_lock)
        {
            return _linkedList.Select(item => item.GameId).ToList();
        }
    }

    public int GetCacheSize()
    {
        lock (_lock)
        {
            return _cache.Count;
        }
    }

    public long GetTotalSizeBytes()
    {
        lock (_lock)
        {
            return _linkedList.Sum(item => item.SizeBytes);
        }
    }

    public void ClearCache()
    {
        var gameIds = new List<string>();
        
        lock (_lock)
        {
            gameIds = _cache.Keys.ToList();
        }

        foreach (var gameId in gameIds)
        {
            RemoveGame(gameId);
        }
        
        _logger.LogInformation("Cleared entire LRU cache");
    }

    private static long CalculateDirectorySize(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return 0;

        try
        {
            return Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                .Sum(file => new FileInfo(file).Length);
        }
        catch
        {
            return 0;
        }
    }

    // Statistics and monitoring
    public CacheStatistics GetStatistics()
    {
        lock (_lock)
        {
            return new CacheStatistics
            {
                TotalGames = _cache.Count,
                TotalSizeBytes = _linkedList.Sum(item => item.SizeBytes),
                LeastRecentlyUsed = _linkedList.Last?.Value.GameId,
                MostRecentlyUsed = _linkedList.First?.Value.GameId,
                CacheHits = _cache.Count,
                MaxCapacity = MaxCapacity
            };
        }
    }
}

public sealed class CacheStatistics
{
    public int TotalGames { get; set; }
    public long TotalSizeBytes { get; set; }
    public string? LeastRecentlyUsed { get; set; }
    public string? MostRecentlyUsed { get; set; }
    public int CacheHits { get; set; }
    public int MaxCapacity { get; set; }
    public double UtilizationPercent => (double)TotalGames / MaxCapacity * 100;
}