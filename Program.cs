using gitlab;

internal class Program
{
    static void build(IBuilder b)
        {
            b.job("JobName", (ctx) =>
            {
                // steps are executed in order
                ctx.step("Stage1", (step_context) =>
                        {
                            // Tasks are executed paralelly
                            step_context.task("ATask", (ctx) => ctx.sh("echo foo"));
                            step_context.task("SecondTask", (ctx) => ctx.sh("echo foo"));
                        }
                    );
                ctx.step("Stage2", (step_context) =>
                        {
                            step_context.task("AnotherTask", (ctx) => ctx.sh("echo foo"));
                        }
                    );
            }
            );
        }
    private static void Main(string[] args)
    {
        var b = new Builder();
        build(b);
        Console.Write(b.ToString());
    }
}