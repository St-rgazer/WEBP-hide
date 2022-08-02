namespace Squisher;

public class Squish
{
    
    public static void Main(string[] args)
    {
        if (args.Length == 0) { Console.WriteLine($"Correct Usage: {selfName}.exe (file) || Drag 'n Drop file onto exe"); Console.ReadKey(); Environment.Exit(0); }
        videoPath = args[0];
        newVideo = System.IO.Path.GetFileNameWithoutExtension(videoPath) + " hidden.webm";
        Console.WriteLine($"Video: {args[0]}");
        CreateTemp();
        Console.WriteLine($"Output: {squishWebm()}");
        DeleteTemp();
    }

    public static string squishWebm()
    {
        pProcess.StartInfo.WorkingDirectory = dir;
        pProcess.StartInfo.FileName = "ffprobe";
        pProcess.StartInfo.Arguments = $"-v error -select_streams v -of json -show_entries stream=r_frame_rate,width,height \"{videoPath}\"";
        pProcess.StartInfo.UseShellExecute = false;
        pProcess.StartInfo.RedirectStandardOutput = true;
        pProcess.Start();
        string strOutput = pProcess.StandardOutput.ReadToEnd();
        dynamic videoInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(strOutput);
        newHeight = (int)videoInfo.streams[0].height;
        string framePrecise = videoInfo.streams[0].r_frame_rate;
        string[] frameSplit = framePrecise.Split("/");
        frameRate = int.Parse(frameSplit[0]) / int.Parse(frameSplit[1]);
        Console.WriteLine($"Video Framerate: {frameRate}");
        Console.WriteLine($"Video Height: {videoInfo.streams[0].height}");
        pProcess.StartInfo.RedirectStandardOutput = false;
        pProcess.StartInfo.CreateNoWindow = true;
        pProcess.StartInfo.FileName = "ffmpeg";
        Console.WriteLine("Splitting audio into temporary file.");
        pProcess.StartInfo.Arguments = $"-y -i \"{videoPath}\" -vn -c:a libvorbis \"{audio}\"";
        pProcess.Start();
        pProcess.WaitForExit();
        if (!File.Exists(audio))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Video has no audio.");
            Console.ForegroundColor = ConsoleColor.White;
            hasAudio = false;
        }
        Console.WriteLine("Splitting frames");
        pProcess.StartInfo.Arguments = $"-y -i \"{videoPath}\" \"{frameName}\"";
        pProcess.Start();
        pProcess.WaitForExit();
        frameCount = Directory.GetFiles(frames, "*.png", SearchOption.TopDirectoryOnly).Length;
        Console.WriteLine($"Frames: {frameCount}"); 
        var files = Directory.GetFiles(frames, "*")
         .Select(file => new { FileName = file, FileNumber = long.Parse(Path.GetFileNameWithoutExtension(file)) })
         .OrderBy(data => data.FileNumber);
        newHeight = (int)videoInfo.streams[0].height;
        foreach (var file in files)
        {
            frame++;
            string fileName = file.FileNumber + ".png";
            
            if (frame > 1)
            {
                newHeight = 1;
            }
            concatFile += $"file \'{Path.Combine(squished, fileName.Substring(0, fileName.Length - 4) + ".webm")}\'\n";
            pProcess.StartInfo.Arguments = $"-y -i \"{Path.Combine(frames, fileName)}\" -c:v vp8 -b:v 1M -crf 10 -vf scale={videoInfo.streams[0].width}x{newHeight} -aspect {videoInfo.streams[0].width}:{newHeight} -r {frameRate} -f webm \"{Path.Combine(squished, fileName.Substring(0, fileName.Length - 4) + ".webm")}\"";
            pProcess.Start();
            // Slower but will ensure each frame is done before moving on.
            pProcess.WaitForExit();
            // Faster but might fail if frames aren't done in time.
            // Thread.Sleep(100);
            Console.WriteLine($"{fileName} | New Height: {newHeight} | Scale: {videoInfo.streams[0].width}x{newHeight} | Aspect: {videoInfo.streams[0].width}:{newHeight} | Framerate: {frameRate}");
        }
        File.WriteAllText(concat, concatFile.TrimEnd('\r', '\n'));
        string audioText = hasAudio ? " and audio." : ".";
        Console.WriteLine($"Combining squished frames{audioText}");
        if (hasAudio) { pProcess.StartInfo.Arguments = $"-y -f concat -safe 0 -i \"{concat}\" -i \"{audio}\" -c copy \"{newVideo}\""; }
        if (!hasAudio){ pProcess.StartInfo.Arguments = $"-y -f concat -safe 0 -i \"{concat}\" -c copy \"{newVideo}\""; }
        pProcess.Start();
        pProcess.WaitForExit();
        return newVideo;
    }

    public static void CreateTemp()
    {
        Console.WriteLine("Creating temp directory");
        if (Directory.Exists(temp))
            DeleteDirectory(temp);
        if (!Directory.Exists(temp)) 
            Directory.CreateDirectory(temp);
        if (!Directory.Exists(frames)) 
            Directory.CreateDirectory(frames);
        if (!Directory.Exists(squished)) 
            Directory.CreateDirectory(squished);
    }

    public static void DeleteTemp()
    {
        if (Directory.Exists(temp))
        { 
            Console.WriteLine("Deleting temp"); 
            DeleteDirectory(temp); 
        }
    }

    public static void DeleteDirectory(string target_dir)
    {
        string[] files = Directory.GetFiles(target_dir);
        string[] dirs = Directory.GetDirectories(target_dir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectory(dir);
        }

        Directory.Delete(target_dir, false);
    }
    public static string dir = AppDomain.CurrentDomain.BaseDirectory;
    public static string temp = Path.Combine(dir, "temp");
    public static string concat = Path.Combine(temp, "concat.txt");
    public static string frames = Path.Combine(temp, "frames");
    public static string squished = Path.Combine(temp, "squishedframes");
    public static string audio = Path.Combine(temp, "audio.webm");
    public static string frameName = Path.Combine(frames, "%d.png");
    public static string selfName = Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
    public static string videoPath;
    public static string newVideo;
    public static System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
    public static bool hasAudio = true;
    public static int frameCount;
    public static int squishTime;
    public static int newHeight;
    public static int frameRate;
    public static string concatFile;
    public static int frame = 0;


}