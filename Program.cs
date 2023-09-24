using gitlab;

internal class Program
{
    static void build(IBuilder b)
        {
            b.job("JobName", (ctx) =>
            {
                IArtifact stage1Artifact = null;

                // steps are executed in order
                ctx.step("Stage1", (step_context) =>
                        {
                            // Tasks are executed paralelly
                            step_context.task("ATask", (ctx) => ctx.sh("echo foo"));
                            step_context.task("SecondTask", (ctx) => 
                            {
                                ctx.sh("echo foo");
                                stage1Artifact = ctx.produceArtifact("P1", new List<string>(){"./out"});
                            });
                            step_context.task("ThirdTask", (ctx) => 
                            {
                                // using the produced artifact in a step that
                                // differs from the one that produced it will
                                // generate a dependency to the producing step,
                                // effectively executing the producing step before
                                // the consuming step.
                                // Also note: The static analysis will trip here, as
                                // it has no means of detecting, that the artifact
                                // variable was actually assigned in "SecondTask".
                                ctx.consumeArtifact(stage1Artifact);    // ensures artifact is available
                                ctx.sh($"echo {stage1Artifact.producedBy.Name}");
                                ctx.sh("echo foo");
                            });                            
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