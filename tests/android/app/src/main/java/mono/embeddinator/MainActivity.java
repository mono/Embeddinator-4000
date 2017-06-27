package mono.embeddinator.testrunner;

import android.content.Intent;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import managed_dll.Native_managed_dll;
import mono.embeddinator.android.ActivitySubclass;

public class MainActivity extends AppCompatActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
    }

    @Override
    protected void onResume() {
        super.onResume();

        Native_managed_dll.INSTANCE.toString(); //TODO: figure out how to remove this
        Intent intent = new Intent(this, ActivitySubclass.class);
        startActivity(intent);
    }
}
