using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MediaFileFormatTests
{
    private string testSceneName = "PlayModeTestsScene";
    private string videoFilesFolder = Application.dataPath + "/PlayModeTests/VideoFiles";

    private LibVLC libVLC;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Core.Initialize(Application.dataPath); // Load VLC dlls
        libVLC = new LibVLC(enableDebugLogs: true);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        libVLC.Dispose();
    }

    [UnityTest]
    public IEnumerator PlayVideoFilesTest()
    {
        SceneManager.LoadScene(testSceneName, LoadSceneMode.Single);

        string[] videoFiles = Directory.GetFiles(videoFilesFolder, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path => !path.Contains(".meta"))
            .ToArray();

        foreach (var videoFile in videoFiles)
        {
            yield return PlayVideoFileTest(videoFile);
        }

        // Wait until disposing of MediaPlayer instances has completed
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator PlayVideoFileTest(string videoFile)
    {
        MediaPlayer mediaPlayer = new MediaPlayer(libVLC);
        mediaPlayer.Play(new Media(videoFile));

        float maxWaitTimeInSeconds = 10;
        float startTime = Time.time;
        yield return new WaitUntil(() =>
        {
            if (Time.time - startTime > maxWaitTimeInSeconds)
            {
                throw new TimeoutException($"Duration not available after {maxWaitTimeInSeconds}");
            }
            return mediaPlayer.Media.Duration > 0;
        });

        Debug.Log($"Duration of file {Path.GetFileName(videoFile)}: {mediaPlayer.Media.Duration} ms");

        // Does not work, libVLC crashes ( https://code.videolan.org/videolan/vlc-unity/-/issues/208 )
        // DisposeMediaPlayerImmediately(mediaPlayer);

        // Does work.
        DisposeMediaPlayer(mediaPlayer);
    }

    private void DisposeMediaPlayerImmediately(MediaPlayer mediaPlayer)
    {
        IntPtr mediaPlayerNativeReference = mediaPlayer.NativeReference;
        string mediaUrl = mediaPlayer.Media?.Mrl;
        Debug.Log($"Disposing Vlc MediaPlayer (Media: '{mediaUrl}', NativeReference: {mediaPlayerNativeReference})");

        mediaPlayer.Dispose();
    }

    private void DisposeMediaPlayer(MediaPlayer mediaPlayer)
    {
        IntPtr mediaPlayerNativeReference = mediaPlayer.NativeReference;
        string mediaUrl = mediaPlayer.Media?.Mrl;
        Debug.Log($"Disposing Vlc MediaPlayer (Media: '{mediaUrl}', NativeReference: {mediaPlayerNativeReference})");

        // TODO: Workaround to make crash in libVLC less likely ( https://code.videolan.org/videolan/vlc-unity/-/issues/208 ).
        // The crash does not occur when sleeping long enough BEFORE disposing the object.
        // Thus, maybe loading the media is not done yet?
        // But how to know when the object is ready to be disposed?
        Task.Run(() =>
        {
            int sleepTimeInMillis = 500;
            Debug.Log($"Sleeping {sleepTimeInMillis} ms before disposing Vlc MediaPlayer to make crash in libVLC less likely (Media: '{mediaUrl}', NativeReference: {mediaPlayerNativeReference})");
            Thread.Sleep(sleepTimeInMillis);

            mediaPlayer.Dispose();

            Debug.Log($"Successfully disposed Vlc MediaPlayer (Media: '{mediaUrl}', NativeReference: {mediaPlayerNativeReference})");
        });
    }
}
