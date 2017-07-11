package mono.embeddinator;

import android.content.pm.*;

/* We have to get the main Application Context for Xamarin.Android to work, this content provider passes it to AndroidImpl for later use */
public class AndroidRuntimeProvider extends android.content.ContentProvider {
    @Override
    public boolean onCreate () {
        return true;
    }

    @Override
    public void attachInfo (android.content.Context context, android.content.pm.ProviderInfo info) {
        Runtime.setImplementation(new AndroidImpl(context));

        //NOTE: this looks a bit weird, but here is what is happening
        //  We put the name of the main class, "managed.Native_managed" for example, in AndroidManifest.xml metadata as mono.embeddinator.classname
        //  We need managed.Native_managed's static constructor to run, we can do this via Class.forName
        try {
            ApplicationInfo appInfo = context.getPackageManager().getApplicationInfo(context.getPackageName(), PackageManager.GET_META_DATA);
            String className = appInfo.metaData.getString("mono.embeddinator.classname");
            Class.forName(className);
        } catch (PackageManager.NameNotFoundException e) {
            throw new RuntimeException(e);
        } catch (ClassNotFoundException e) {
            throw new RuntimeException(e);
        }

        super.attachInfo (context, info);
    }

    @Override
    public android.database.Cursor query (android.net.Uri uri, String[] projection, String selection, String[] selectionArgs, String sortOrder) {
        throw new RuntimeException ("This operation is not supported.");
    }

    @Override
    public String getType (android.net.Uri uri) {
        throw new RuntimeException ("This operation is not supported.");
    }

    @Override
    public android.net.Uri insert (android.net.Uri uri, android.content.ContentValues initialValues) {
        throw new RuntimeException ("This operation is not supported.");
    }

    @Override
    public int delete (android.net.Uri uri, String where, String[] whereArgs) {
        throw new RuntimeException ("This operation is not supported.");
    }

    @Override
    public int update (android.net.Uri uri, android.content.ContentValues values, String where, String[] whereArgs) {
        throw new RuntimeException ("This operation is not supported.");
    }
}
