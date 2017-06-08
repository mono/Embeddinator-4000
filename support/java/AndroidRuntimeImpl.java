package mono.embeddinator;

import android.content.*;
import android.content.pm.*;
import com.sun.jna.*;
import java.util.*;
import mono.embeddinator.Runtime.RuntimeLibrary;

public class AndroidRuntimeImpl extends RuntimeImpl {
    interface AndroidRuntime extends com.sun.jna.Library {
        public void Java_mono_android_Runtime_init(String lang, String[] runtimeApks, String runtimeDataDir, String[] appDirs, ClassLoader loader, String externalStorageDir, String[] assemblies, String packageName);
    }

    private AndroidRuntime androidRuntime;

    //TODO: currently setting this via getApplicationContext() in Android app
    public static Context context;

    @Override
    public RuntimeLibrary initialize(String library) {
        androidRuntime = Native.loadLibrary("monodroid", AndroidRuntime.class);

        ApplicationInfo app = context.getApplicationInfo();
        Locale locale       = Locale.getDefault ();
        String language     = locale.getLanguage () + "-" + locale.getCountry ();
        String filesDir     = context.getFilesDir ().getAbsolutePath ();
        String cacheDir     = context.getCacheDir ().getAbsolutePath ();
        String dataDir      = app.nativeLibraryDir;
        ClassLoader loader  = context.getClassLoader ();

        androidRuntime.Java_mono_android_Runtime_init (
                language,
                new String[] { app.sourceDir },
                app.nativeLibraryDir,
                new String[]{
                        filesDir,
                        cacheDir,
                        dataDir,
                },
                loader,
                new java.io.File (
                        android.os.Environment.getExternalStorageDirectory (),
                        "Android/data/" + context.getPackageName () + "/files/.__override__").getAbsolutePath (),
                new String[] {
                        "managed.dll",
                        "mscorlib.dll",
                        "System.Core.dll"
                },
                context.getPackageName ());

        RuntimeLibrary runtimeLibrary = Native.loadLibrary(library, RuntimeLibrary.class);

        return runtimeLibrary;
    }

    @Override
    public String getResourcePath(String library) {
        return "/assets/assemblies/" + library + ".dll";
    }
}
