namespace AOMBC;

using Xunit;

public class AOMBCTests
{
    [Fact]
    public void MainTest()
    {
        Assert.True(AOMBC.Program.IsBalanced("a+(b*c)-2-a", "()"));
        Assert.True(AOMBC.Program.IsBalanced("[a+b*(2-c)-2+a]*2", "()[]"));
        Assert.False(AOMBC.Program.IsBalanced("[a+b*(2-c]-2+a)*2", "()[]"));
        Assert.True(AOMBC.Program.IsBalanced("Sensei says -yes-!", "--"));

        Assert.False(AOMBC.Program.IsBalanced("(Hello Mother can you hear me?))", "()"));
        Assert.False(AOMBC.Program.IsBalanced("Hello Mother can you hear me?)[Monkeys, in my pockets!!]", "()[]"));
        Assert.False(AOMBC.Program.IsBalanced("(()Hello()))", "()"));
    }
}
