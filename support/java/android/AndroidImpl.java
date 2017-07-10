package mono.embeddinator;

import android.content.*;
import android.content.pm.*;
import com.sun.jna.*;
import java.util.*;

import mono.embeddinator.Runtime.RuntimeLibrary;

public class AndroidImpl extends DesktopImpl {
    private final Context context;

    public AndroidImpl(Context context) {
        this.context = context;
        mono.MonoPackageManager.Context = context;
    }

    @Override
    public RuntimeLibrary initialize(String library) {
        android.content.IntentFilter timezoneChangedFilter  = new android.content.IntentFilter (
                android.content.Intent.ACTION_TIMEZONE_CHANGED
        );
        context.registerReceiver (new mono.android.app.NotifyTimeZoneChanges(), timezoneChangedFilter);

        System.loadLibrary("monodroid");
        System.loadLibrary(library);
        setAssemblyPrefix();

        ApplicationInfo app = context.getApplicationInfo();
        Locale locale       = Locale.getDefault ();
        String language     = locale.getLanguage () + "-" + locale.getCountry ();
        String filesDir     = context.getFilesDir ().getAbsolutePath ();
        String cacheDir     = context.getCacheDir ().getAbsolutePath ();
        String dataDir      = app.nativeLibraryDir;
        ClassLoader loader  = context.getClassLoader ();

        mono.android.Runtime.init (
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
                        library + ".dll",
                        "Resource.designer.dll"
                },
                context.getPackageName ());

        RuntimeLibrary runtimeLibrary = Native.loadLibrary(library, RuntimeLibrary.class);

        return runtimeLibrary;
    }

    private static native void setAssemblyPrefix();
}
