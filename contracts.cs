
delegate void jobfunc(IContext param);
delegate void anyFunc();

interface IBuilder
{
    IJob job(string JobName, jobfunc func); 
}

interface IJob
{
    IJob andThen(jobfunc func);
}

delegate void stagefunc(IStageContext contxt);

interface IContext
{
    IContext stage(string stageName, stagefunc func);
    IContext andThen(anyFunc func);
}

interface IStageContext
{
    void sh(string command);
}
