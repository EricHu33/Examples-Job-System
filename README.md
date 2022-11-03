# Example_Job_System
Example of using dfiffrent Job of Unity C# Job System

<img src="https://user-images.githubusercontent.com/13420668/199733887-f2de99b2-3f08-465a-b04f-7d7d4ad46533.png" width="350">

Enable differnt script one at a time and using profiler to check the difference.

## NoiseMotionMainThread
Traditional update, main thread code, no job used.

SingleJobDemo
- Example of using IJob

SingleForJobDemo
- Example of using IJobFor

ParallelForJobDemo
- Example of using IJobParallelFor

ParallelForFilterJobDemo
- Example of using IJobParallelForFilter, IJobParallelForDelay
  Move the secetion area object to different cubes being highlight by the selection box.

ParallelForJobInstancingDemo
 - Example of using IJobParallelFor and Graphics.DrawInstanced()
