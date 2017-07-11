package mono.embeddinator;

import static org.junit.Assert.*;
import org.junit.*;
import org.junit.runner.RunWith;
import android.app.Activity;
import android.view.View;
import android.widget.TextView;
import android.content.*;
import android.support.test.rule.ActivityTestRule;
import android.support.test.runner.AndroidJUnit4;
import java.io.*;
import mono.embeddinator.testrunner.MainActivity;
import mono.embeddinator.android.*;

@RunWith(AndroidJUnit4.class)
public class AndroidTests {
    //We need an activity for these tests
    @Rule
    public ActivityTestRule rule = new ActivityTestRule<>(MainActivity.class);

    @Test
    public void createView() throws Throwable {
        ViewSubclass v = new ViewSubclass(rule.getActivity());
        assertNotNull(v);
    }

    @Test
    public void callExports() throws Throwable {
        String expected = "Hello";
        ViewSubclass v = new ViewSubclass(rule.getActivity());
        v.apply(expected);
        assertEquals(expected, v.getText());
    }

    @Test
    public void buttonClick() throws Throwable {
        ButtonSubclass b = new ButtonSubclass(rule.getActivity());
        b.performClick();
        assertEquals(1, b.getTimes());
    }

    @Test
    public void startActivity() throws Throwable {
        Activity activity = rule.getActivity();
        Intent intent = new Intent(activity, ActivitySubclass.class);
        activity.startActivity(intent);
    }

    @Test
    public void resourceString() {
        String actual = rule.getActivity().getResources().getString(managed.R.string.hello);
        assertEquals("Hello from C#!", actual);
    }

    @Test
    public void resourceLayout() {
        View view = rule.getActivity().getLayoutInflater().inflate(managed.R.layout.hello, null);
        assertNotNull(view);
    }

    @Test
    public void resourceId() {
        View view = rule.getActivity().getLayoutInflater().inflate(managed.R.layout.hello, null);
        TextView text = (TextView)view.findViewById(managed.R.id.text);
        assertNotNull(text);
        assertEquals("Hello from C#!", text.getText());
    }

    @Test
    public void resourceCustomView() {
        TextView text = (TextView)rule.getActivity().getLayoutInflater().inflate(managed.R.layout.customview, null);
        assertEquals("World!", text.getText());
    }

    @Test
    public void asset() throws IOException {
        InputStream stream = rule.getActivity().getAssets().open("test.txt");
        BufferedReader reader = new BufferedReader(new InputStreamReader(stream));
        try {
            String text = reader.readLine();
            assertTrue(text.contains("This is an asset"));
        } finally {
            reader.close();
        }
    }

    @Test
    public void applicationContext() {
        AndroidAssertions.applicationContext();
    }

    @Test
    public void asyncAwait() {
        AndroidAssertions.asyncAwait();
    }

    @Test
    public void webRequest() {
        AndroidAssertions.webRequest();
    }

    @Test
    public void callIntoSupportLibrary() {
        AndroidAssertions.callIntoSupportLibrary();
    }
}
