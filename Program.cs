using SchemaGenerator;


internal class Program
{
    private static void Main(string[] args)
    {
        if (SchemaGenHandler.TakeInputs(args, out string filename, out string path, out List<string> inputs))
        {
            SchemaGenHandler.GenerateSchema(filename, path, inputs);
        }
    }
}
