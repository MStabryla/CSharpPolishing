using System.Threading;

class NumbersCounter
{
    private int _oddCounter;
    private int _evenCounter;

    private SemaphoreSlim _oddSemaphore = new SemaphoreSlim(1);
    
    private SemaphoreSlim _evenSemaphore = new SemaphoreSlim(0);

    public NumbersCounter()
    {
        _oddCounter = 1;
        _evenCounter = 2;
    }

    public void PrintOddNumber()
    {
        _oddSemaphore.Wait();
        Console.WriteLine(_oddCounter);
        _oddCounter += 2;
        _evenSemaphore.Release();
    }

    public void PrintEvenNumber()
    {
        _evenSemaphore.Wait();
        Console.WriteLine(_evenCounter);
        _evenCounter += 2;
        _oddSemaphore.Release();
    }
}


class Program
{
    static void Main(string[] args)
    {   
        Console.WriteLine("Odd Even");
        int taskCounter = 10;
        NumbersCounter numbersCounter = new();

        Task oddNumber = new(() =>
        {
            
            for (int i = 0; i < taskCounter; i++)
            {
                numbersCounter.PrintOddNumber();
            }
        });
        Task evenNumber = new(() =>
        {
            for (int i = 0; i < taskCounter; i++)
            {
                numbersCounter.PrintEvenNumber();
            }
        });

        evenNumber.Start();
        oddNumber.Start();

        evenNumber.Wait();
        oddNumber.Wait();
        
    }
}
