namespace AOMBC;

public class Program
{
    public static bool IsBalanced(string text, string brackets)
    {
        if(brackets.Length % 2 != 0)
            throw new ArgumentException("Brackets string must contain even number of characters."); 
        
        (char,char)[] availableBrackets = new (char,char)[brackets.Length / 2];
        for(int j = 0;j<brackets.Length;j+=2)
        {
            availableBrackets[j/2] = (brackets [j],brackets[j+1]);
        }
            
        Stack<(char,char)> bracketsStack = new Stack<(char,char)>();

        for(int i = 0;i < text.Length;i++)
        {
            char actChar = text[i];
            if(brackets.Contains(actChar))
            {
                
                if(bracketsStack.Count > 0)
                {
                    var actBracket = bracketsStack.Peek();
                    //opening bracket
                    if(availableBrackets.FirstOrDefault(x => x.Item1 == actChar && actBracket.Item2 != actChar) != default) 
                    {
                        actBracket = availableBrackets.First(x => x.Item1 == actChar);
                        bracketsStack.Push(actBracket);
                    }
                    //closing bracket
                    else if(availableBrackets.FirstOrDefault(x => x.Item2 == actChar) != default)
                    {
                        bracketsStack.Pop();
                        if(actBracket.Item2 != actChar)
                            return false;
                    }
                }
                else
                {
                    if(availableBrackets.FirstOrDefault(x => x.Item1 == actChar) != default) 
                    {
                        var actBracket = availableBrackets.First(x => x.Item1 == actChar);
                        bracketsStack.Push(actBracket);
                    }
                    else if(availableBrackets.FirstOrDefault(x => x.Item2 == actChar) != default)
                    {
                        return false;
                    }
                }
                
            }
        }
        if(bracketsStack.Count > 0)
            return false;
        return true;
    }



    static void Main(string[] args)
    {
        
    }
}