// using gitlab;
using local;

internal class Program
{
    static void build(IBuilder b)
        {
            b.job("JobName", (ctx) =>
            {
                IArtifact stage1Artifact = null;
                IArtifact secondArtifact = null;

                // steps are executed in order
                ctx.step("Stage1", (step_context) =>
                        {
                            // Tasks are executed paralelly for most CI servers,
                            // but sequentially for local builds
                            step_context.task("ATask", (ctx) => ctx.sh("echo Task1 done"));
                            step_context.task("SecondTask", (ctx) => 
                            {
                                ctx.sh("echo SecondTask done");
                                stage1Artifact = ctx.produceArtifact("P1", new List<string>(){"./out"});
                            }).andThen("ThirdTask", (ctx) => 
                                {
                                    ctx.sh("echo Enter Task 3");
                                    // using the produced artifact in a step that
                                    // differs from the one that produced it will
                                    // generate a dependency to the producing step,
                                    // effectively executing the producing step before
                                    // the consuming step.
                                    // Also note: The static analysis will trip here, as
                                    // it has no means of detecting, that the artifact
                                    // variable was actually assigned in "SecondTask".
                                    ctx.consumeArtifact(stage1Artifact);    // ensures artifact is available
                                    
                                    // While this works, it does not ensure, that the artifact
                                    // is actually present during the execution of the
                                    // task. To have it present it must be "consumed" by the task.
                                    ctx.sh($"echo Using Artifact produced by: {stage1Artifact.producedBy.Name}");   
                                    ctx.sh("echo ThirdTask done");
                                    secondArtifact = ctx.produceArtifact("P2", new List<string>(){"./out"});
                                
                                }).andThen("LastTask", (ctx) =>
                                {
                                     ctx.sh("echo Enter Task 4");
                                     ctx.consumeArtifact(secondArtifact);
                                });
                        }
                    );
                ctx.step("Stage2", (step_context) =>
                        {
                            step_context.task("AnotherTask", (ctx) => ctx.sh("echo Stage 2, another task"));
                        }
                    );
            }
            );
        }
    private static void Main(string[] args)
    {
        var b = new local.Builder();
        build(b);
        Console.Write(b.ToString());
    }
}