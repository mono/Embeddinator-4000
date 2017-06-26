package mono.embeddinator;

import static org.junit.Assert.*;
import org.junit.*;
import org.junit.runner.RunWith;
import android.support.test.rule.ActivityTestRule;
import android.support.test.runner.AndroidJUnit4;
import managed_dll.Native_managed_dll;
import mono.embeddinator.ViewSubclass;
import mono.embeddinator.testrunner.MainActivity;

@RunWith(AndroidJUnit4.class)
public class AndroidTests {
    //We need an activity for these tests
    @Rule
    public ActivityTestRule rule = new ActivityTestRule<>(MainActivity.class);

    @Before
    public void setUp() {
        //NOTE: this forces the tests to run in the same context an app referencing the AAR would
        //  Instrumented tests do not seem to follow the pattern of loading content providers, etc.
        Runtime.setImplementation(new AndroidImpl(rule.getActivity()));
        assertNotNull(Native_managed_dll.INSTANCE);
    }

    @Test
    public void createView() throws Throwable {
        ViewSubclass v = new ViewSubclass(rule.getActivity());
        assertNotNull(v);
    }

    @Test
    public void callExports() throws Throwable {
        String expected = "Hello";
        ViewSubclass v = new ViewSubclass(rule.getActivity());
        v.set_TextToApply(expected);
        v.Apply();
        assertEquals(expected, v.getText());
    }
}
