using System.Text.Json;

namespace AsyncAwait;

public class AsyncAwaitProgram
{
    private static string _apiPath = "https://jsonplaceholder.typicode.com/";
    public static async Task Main(string[] arguments)
    {
        Console.WriteLine(await QueryPosts());
        // Console.WriteLine(await TestConnector());
    }

    public static async Task<string?> TestConnector()
    {
        string? s;
        using(AsyncConnector connector = new AsyncConnector(_apiPath))
        {
            var data = await connector.RetrieveStringData("/posts");
            s = data != null ? data.ToString() : "empty";
            Console.WriteLine($"DEBUG: \n{data}\n");
        }
        return s;
    }

    public static async Task<string> QueryPosts()
    {
        Console.WriteLine("|Query started|");
        string output = "";
        using (AsyncConnector connector = new AsyncConnector(_apiPath))
        {
            // CancellationToken canTok = new CancellationToken();
            Task ret1 = new( async () => {
                Console.WriteLine("ret1 start");
                for(int i = 1; i< 3;i++)
                {
                    await Task.Delay(Random.Shared.Next(500,1500));
                    var task = connector.RetrieveData("/posts");
                    task.Wait();
                    output += $"\n---\n{TaskToStringOutput(task)}\n---\n";
                        
                }
            });

            async Task PostTask(int i)
            {
                Console.WriteLine($"Post Query {i}");
                var task = await connector.RetrieveData($"/posts/{i}");
                output += $"\n---\n{ObjToStringOutput(task)}\n---\n";
            }
            Task ret2 = new( async () => {
                Console.WriteLine("ret2 start");
                Task[] taskArray = new Task[15];
                for(int i=0; i < 15; i++)
                {
                    taskArray[i] = PostTask(i);
                }
                Task.WaitAll(taskArray); 
            });

            async Task PostCommentTask(int i)
            {
                Console.WriteLine($"Comment Query {i}");
                var task = await connector.RetrieveData($"/posts/{i}/comments");
                output += $"\n---\n{ObjToStringOutput(task)}\n---\n";
            }
            Task ret3 = new( async () => {
                Console.WriteLine("ret3 start");
                Task[] taskArray = new Task[15];
                for(int i=0; i < 15; i++)
                {
                    taskArray[i] = PostCommentTask(i);
                }
                Task.WaitAll(taskArray);
            });

            ret3.Start();
            ret2.Start();
            ret1.Start();

            await ret1;
            await ret2;
            await ret3;

        }
        Console.WriteLine("|Query ended|");
        return output;
    }

    private static string TaskToStringOutput(Task<object> task)
    {
        var dat = task.Result;
        if(dat is object[] v)
        {
            return JsonSerializer.Serialize(v.Take(5));
        }
        else if(dat != null)
        {
            string datString = JsonSerializer.Serialize(dat) ?? "";
            return new string(datString.Take(150).ToArray());
        }
        return "";
    }

    private static string ObjToStringOutput(object dat)
    {
        if(dat is object[] v)
        {
            return JsonSerializer.Serialize(v.Take(5));
        }
        else if(dat != null)
        {
            string datString = JsonSerializer.Serialize(dat) ?? "";
            return new string(datString.Take(150).ToArray());
        }
        return "";
    }
}