using gitlab;

internal class Program
{
    static void build(IBuilder b)
        {
            b.job("JobName", (ctx) =>
            {
                ctx.stage("AStage", (stage_context) =>
                        {
                            stage_context.sh("echo foo");
                        }
                    ).stage("Stage2", (stage_context) =>
                        {
                            stage_context.sh("echo foo");
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