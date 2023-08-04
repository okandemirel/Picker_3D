namespace Runtime.Interfaces
{
    public interface ICommand
    {
        public void Execute(int parameter) { }
        public void Execute() { }
    }
}