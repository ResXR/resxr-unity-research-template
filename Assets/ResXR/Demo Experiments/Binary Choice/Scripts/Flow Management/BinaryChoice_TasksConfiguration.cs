using UnityEngine.Serialization;

public class BinaryChoice_TasksConfiguration : ResXRSingleton<BinaryChoice_TasksConfiguration>
{
    [FormerlySerializedAs("rounds")]
    public BinaryChoice_Task[] tasks;
}
