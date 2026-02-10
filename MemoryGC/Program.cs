namespace MemoryGC;

using System.Buffers;
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
    private static DefaultObjectPool<FoodData> _foodDataPool = new(new DefaultPooledObjectPolicy<FoodData>());
    private static ArrayPool<FoodData> _foodDataArrayPool = ArrayPool<FoodData>.Shared;
    private static ArrayPool<FoodData> _foodCreatedDataArrayPool = ArrayPool<FoodData>.Create(31,1);

    private static void GenFood(uint index)
    {
        var obj = _foodDataPool.Get();
        obj.Id = index;
        obj.ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(Random.Shared.Next(28)));
        Console.WriteLine($"Allocating object with id {index} and expDate {obj.ExpirationDate.Month}/{obj.ExpirationDate.Day}");
        _foodDataPool.Return(obj);

    }

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
        Console.WriteLine("MemoryCG start");

        var foodRef = _foodDataPool.Get();
        foodRef.ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(14));
        _foodDataPool.Return(foodRef);
        Console.WriteLine("Test single allocation");

        Task singlePoolAlloc = new(() => 
        {
            var taskPool = new List<Task>();
            for(uint i = 0; i < 30; i++)
            {
                uint locI = i;
                taskPool.Add(Task.Run(() => GenFood(locI)));
            }
        }
        );
        Task arrayPoolAlloc = new(() =>
        {   
            FoodData[] arr = _foodDataArrayPool.Rent(30);
            for(uint i = 0; i < (arr ?? []).Length; i++)
            {
                uint locI = i;
                if(arr != null && arr.Length > i)
                {
                    arr[locI] = ArrayGenFood(locI);
                    Console.WriteLine($"Allocating object in array with id {arr[locI].Id} and expDate {arr[locI].ExpirationDate.Month}/{arr[locI].ExpirationDate.Day}");
                }
            }
            _foodDataArrayPool.Return(arr ?? [], false);
        });
        Task arrayCreatedPoolAlloc = new(() =>
        {   
            FoodData[] arr = _foodCreatedDataArrayPool.Rent(30);
            for(uint i = 0; i < (arr ?? []).Length; i++)
            {
                uint locI = i;
                if(arr != null && arr.Length > i)
                {
                    
                    arr[locI] = ArrayGenFood(locI);
                    Console.WriteLine($"Allocating object in array with id {arr[locI].Id} and expDate {arr[locI].ExpirationDate.Month}/{arr[locI].ExpirationDate.Day}");
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

        Console.WriteLine("ArrayPool Test");
        FoodData[] array = _foodDataArrayPool.Rent(30);
        for(int i = 0; i < array.Length; i++)
        {
            if(array[i] == null)
                Console.WriteLine($"Array index {i} is null");
            else
                Console.WriteLine($"Array index {i} has id {array[i].Id} and expDate {array[i].ExpirationDate.Month}/{array[i].ExpirationDate.Day}");
        }
        FoodData[] createdArray = _foodCreatedDataArrayPool.Rent(30);
        for(int i = 0; i < createdArray.Length; i++)
        {
            if(createdArray[i] == null)
                Console.WriteLine($"Created Array index {i} is null");
            else
                Console.WriteLine($"Created Array index {i} has id {createdArray[i].Id} and expDate {createdArray[i].ExpirationDate.Month}/{createdArray[i].ExpirationDate.Day}");
        }
        if(array != null && array.All(i => i != null && i.Id != uint.MaxValue))
            Console.WriteLine("ArrayPool initialization test passed");
        else
            Console.WriteLine("ArrayPool initialization test failed");

        if(createdArray != null && createdArray.All(i => i != null && i.Id != uint.MaxValue))
            Console.WriteLine("CreatedArrayPool initialization test passed");
        else
            Console.WriteLine("CreatedArrayPool initialization test failed");
        
        if(array != null)
            _foodDataArrayPool.Return(array,false);
        if(createdArray != null)
            _foodCreatedDataArrayPool.Return(createdArray);


        // Memory and Span test

        Span<FoodDataStruct> foodSpan = new(new FoodDataStruct[30]);
        for(int i = 0; i < foodSpan.Length; i++)
        {
            foodSpan[i] = new FoodDataStruct
            {
                Id = (uint)i,
                ExpirationDate = DateOnly.FromDateTime(DateTime.Now.AddDays(Random.Shared.Next(28)))
            };
            Console.WriteLine($"Adding struct to span with id {foodSpan[i].Id} and expDate {foodSpan[i].ExpirationDate.Month}/{foodSpan[i].ExpirationDate.Day}");
        }
        foodSpan.Clear();
    }
}