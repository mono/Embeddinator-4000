package mono.embeddinator;

/* We have to get the main Application Context for Xamarin.Android to work, this content provider stores it in a static variable */
public class AndroidRuntimeProvider extends android.content.ContentProvider {
    @Override
    public boolean onCreate () {
        return true;
    }

    @Override
    public void attachInfo (android.content.Context context, android.content.pm.ProviderInfo info) {
        AndroidImpl.context = context;
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
