namespace MemoryGC;

using System.Buffers;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.Extensions.ObjectPool;

class FoodData
{
    public uint Id { get; set; }
    public string FoodName { get; set; }
    public DateOnly ExpirationDate { get; set; }

    //Weight in kilograms
    public double Weight { get; set; }

    //Price in PLN
    public decimal Price { get; set; }

    public FoodData()
    {
        Id = uint.MaxValue;
        FoodName = string.Empty;
        ExpirationDate = DateOnly.FromDateTime(DateTime.Now);
        Weight = 1.0;
        Price = 1.0m;
    }
}
struct FoodDataStruct
{
    public uint Id { get; set; }
    public string FoodName { get; set; }
    public DateOnly ExpirationDate { get; set; }

    //Weight in kilograms
    public double Weight { get; set; }

    //Price in PLN
    public decimal Price { get; set; }

    public FoodDataStruct()
    {
        Id = uint.MaxValue;
        FoodName = string.Empty;
        ExpirationDate = DateOnly.FromDateTime(DateTime.Now);
        Weight = 1.0;
        Price = 1.0m;
    }
}
public class Program
{
    private const int _tableCount = short.MaxValue;

    private static DefaultObjectPool<FoodData> _foodDataPool = new(new DefaultPooledObjectPolicy<FoodData>());
    private static ArrayPool<FoodData> _foodDataArrayPool = ArrayPool<FoodData>.Shared;
    private static ArrayPool<FoodData> _foodCreatedDataArrayPool = ArrayPool<FoodData>.Create(_tableCount,1);


    private static FoodData ArrayGenFood(uint index)
    {
        
        var newFoodData = new FoodData
        {
            Id = index,
            ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(Random.Shared.Next(28)))
        };
        return newFoodData;
    }


    public static async Task Main(string[] args)
    {
        // Console.WriteLine("ObjectPoolTest start");
        // (bool, long) objectPoolTestResult = await ObjectPoolTest();
        // if(objectPoolTestResult.Item1)
        //     Console.WriteLine($"ObjectPool Test passed with {objectPoolTestResult.Item2} ms");
        // else
        //     Console.WriteLine("ObjectPool test failed");

        // Console.WriteLine("SpanTest start");
        // (bool, long) spanTestResult = await SpanMemoryTest();
        // if(spanTestResult.Item1)
        //     Console.WriteLine($"SpanTest passed with {spanTestResult.Item2} ms");
        // else
        //     Console.WriteLine("SpanTest failed");

        Task<(bool,long,long)>[] taskResults = [
            SpanMemoryTest(),
            NormalArrayTest(),
            NormalListTest()
        ];
        Task.WaitAll(taskResults);
        if(taskResults[0].Result.Item1)
            Console.WriteLine($"SpanTest passed with {taskResults[0].Result.Item2} ms");
        else
        {
            Console.WriteLine("SpanTest failed"); return;
        }

        Console.WriteLine($"Compare time:\nSpanTest: {taskResults[0].Result.Item3} ({taskResults[0].Result.Item2},{taskResults[0].Result.Item3 - taskResults[0].Result.Item2}) ms\nNormalArrayTest: {taskResults[1].Result.Item3} ({taskResults[1].Result.Item2},{taskResults[1].Result.Item3 - taskResults[1].Result.Item2}) ms\nNormalListTest: {taskResults[2].Result.Item3} ({taskResults[2].Result.Item2},{taskResults[2].Result.Item3 - taskResults[2].Result.Item2}) ms");
    }

    private static async Task<(bool,long)> ObjectPoolTest()
    {
        Stopwatch _diagnostic = new();
        _diagnostic.Restart();
        Task arrayPoolAlloc = new(() =>
        {   
            FoodData[] arr = _foodDataArrayPool.Rent(_tableCount);
            for(uint i = 0; i < (arr ?? []).Length; i++)
            {
                uint locI = i;
                if(arr != null && arr.Length > i)
                {
                    arr[locI] = ArrayGenFood(locI);
                    Debug.WriteLine($"Allocating object in array with id {arr[locI].Id} and expDate {arr[locI].ExpirationDate.Month}/{arr[locI].ExpirationDate.Day}");
                }
            }
            _foodDataArrayPool.Return(arr ?? [], false);
        });
        Task arrayCreatedPoolAlloc = new(() =>
        {   
            FoodData[] arr = _foodCreatedDataArrayPool.Rent(_tableCount);
            for(uint i = 0; i < (arr ?? []).Length; i++)
            {
                uint locI = i;
                if(arr != null && arr.Length > i)
                {
                    
                    arr[locI] = ArrayGenFood(locI);
                    Debug.WriteLine($"Allocating object in created array with id {arr[locI].Id} and expDate {arr[locI].ExpirationDate.Month}/{arr[locI].ExpirationDate.Day}");
                }
            }
            _foodCreatedDataArrayPool.Return(arr ?? [], false);
        });

        var tasks = new[]
        {
          arrayPoolAlloc, 
          arrayCreatedPoolAlloc  
        };
        foreach(var t in tasks) { t.Start();}
        
        await Task.WhenAll(tasks);

        FoodData[] sharedArray = ArrayPool<FoodData>.Shared.Rent(_tableCount);
        for(int i = 0; i < sharedArray.Length; i++)
        {
            if(sharedArray[i] == null)
                Debug.WriteLine($"Shared Array index {i} is null");
            else
                Debug.WriteLine($"Shared Array index {i} has id {sharedArray[i].Id} and expDate {sharedArray[i].ExpirationDate.Month}/{sharedArray[i].ExpirationDate.Day}");
        }
        FoodData[] createdArray = _foodCreatedDataArrayPool.Rent(_tableCount);
        for(int i = 0; i < createdArray.Length; i++)
        {
            if(createdArray[i] == null)
                Debug.WriteLine($"Created Array index {i} is null");
            else
                Debug.WriteLine($"Created Array index {i} has id {createdArray[i].Id} and expDate {createdArray[i].ExpirationDate.Month}/{createdArray[i].ExpirationDate.Day}");
        }
        bool testSharedArrayResult,testCreatedArrayResult;
        if(sharedArray != null && sharedArray.All(i => i != null && i.Id != uint.MaxValue))
            testSharedArrayResult = true;
        else
            testSharedArrayResult = false;

        if(createdArray != null && createdArray.All(i => i != null && i.Id != uint.MaxValue))
            testCreatedArrayResult = true;
        else
            testCreatedArrayResult = false;
        
        if(sharedArray != null)
            _foodDataArrayPool.Return(sharedArray,false);
        if(createdArray != null)
            _foodCreatedDataArrayPool.Return(createdArray);
        
        _diagnostic.Stop();
        return (testSharedArrayResult && testCreatedArrayResult, _diagnostic.ElapsedMilliseconds);
    }

    private static async Task<(bool,long,long)> SpanMemoryTest()
    {
        Stopwatch _diagnostic = new();
        _diagnostic.Restart();
        Span<FoodDataStruct> foodSpan = new(new FoodDataStruct[_tableCount]);
        for(int i = 0; i < foodSpan.Length; i++)
        {
            foodSpan[i] = new FoodDataStruct
            {
                Id = (uint)i,
                ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(i % 28 + 1))
            };
            Debug.WriteLine($"Adding struct to span with id {foodSpan[i].Id} and expDate {foodSpan[i].ExpirationDate.Month}/{foodSpan[i].ExpirationDate.Day}");
        }
        long creationTime = _diagnostic.ElapsedMilliseconds;

        bool testSpanResult = true;
        foreach(var i in foodSpan)
        {
            if(i.Id != uint.MaxValue)
                Debug.WriteLine($"Span struct with id {i.Id} and expDate {i.ExpirationDate.Month}/{i.ExpirationDate.Day}");
            else
            {
                Debug.WriteLine($"Span struct with id {i.Id} is null or default");
                testSpanResult = false;
                break;
            }
        }
        foodSpan.Clear();

        _diagnostic.Stop();
        return (testSpanResult, creationTime, _diagnostic.ElapsedMilliseconds);
    }



    private static async Task<(bool,long,long)> NormalArrayTest()
    {
        Stopwatch _diagnostic = new();
        _diagnostic.Restart();
        FoodDataStruct[] foodSpan = new FoodDataStruct[_tableCount];
        for(int i = 0; i < foodSpan.Length; i++)
        {
            foodSpan[i] = new FoodDataStruct
            {
                Id = (uint)i,
                ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(i % 28 + 1))
            };
            Debug.WriteLine($"Adding struct to array with id {foodSpan[i].Id} and expDate {foodSpan[i].ExpirationDate.Month}/{foodSpan[i].ExpirationDate.Day}");
        }
        long creationTime = _diagnostic.ElapsedMilliseconds;

        bool testSpanResult = true;
        foreach(var i in foodSpan)
        {
            if(i.Id != uint.MaxValue)
                Debug.WriteLine($"Array struct with id {i.Id} and expDate {i.ExpirationDate.Month}/{i.ExpirationDate.Day}");
            else
            {
                Debug.WriteLine($"Array struct with id {i.Id} is null or default");
                testSpanResult = false;
                break;
            }
        }

        _diagnostic.Stop();
        return (testSpanResult, creationTime, _diagnostic.ElapsedMilliseconds);
    }

    private static async Task<(bool,long,long)> NormalListTest()
    {
        Stopwatch _diagnostic = new();
        _diagnostic.Start();
        List<FoodDataStruct> foodSpan = [];
        for(int i = 0; i < _tableCount; i++)
        {
            foodSpan.Add(new FoodDataStruct
            {
                Id = (uint)i,
                ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(i % 28 + 1))
            });
            Debug.WriteLine($"Adding struct to list with id {foodSpan[i].Id} and expDate {foodSpan[i].ExpirationDate.Month}/{foodSpan[i].ExpirationDate.Day}");
        }
        long creationTime = _diagnostic.ElapsedMilliseconds;

        bool testSpanResult = true;
        foreach(var i in foodSpan)
        {
            if(i.Id != uint.MaxValue)
                Debug.WriteLine($"List struct with id {i.Id} and expDate {i.ExpirationDate.Month}/{i.ExpirationDate.Day}");
            else
            {
                Debug.WriteLine($"List struct with id {i.Id} is null or default");
                testSpanResult = false;
                break;
            }
        }

        _diagnostic.Stop();
        return (testSpanResult, creationTime, _diagnostic.ElapsedMilliseconds);
    }
}