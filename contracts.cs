
enum TargetType
{
    Gitlab,
    AzureDevOps,
    Local
}

delegate void jobFunc(IJobContext param);
delegate void anyFunc();

interface IBuilder
{
    IJob job(string JobName, jobFunc func); 

    TargetType Target { get; }
}

interface IJob
{
    IJob andThen(jobFunc func);
}

interface IJobContext
{
    IJobContext step(string stepName, stepFunc func);
    void andThen(anyFunc func);
}

delegate void stepFunc(IStepContext contxt);
delegate void taskFunc(ITaskContext contxt);


interface IStepContext
{
    void task(string taskName, taskFunc func);
    IStepContext andThen(anyFunc func);
}

interface ITaskContext
{
    void sh(string command);
    void consumeArtifact(IArtifact artifact);
    IArtifact produceArtifact(string name, List<string> filters);

    string Name{get;}
}

interface IArtifact
{
    ITaskContext producedBy{get;}
    string Name { get; }
}