package mono.embeddinator;

import static org.junit.Assert.*;
import org.junit.*;
import org.junit.runner.RunWith;
import android.content.Intent;
import android.support.test.rule.ActivityTestRule;
import android.support.test.runner.AndroidJUnit4;
import managed_dll.Native_managed_dll;
import mono.embeddinator.testrunner.MainActivity;
import mono.embeddinator.android.*;

@RunWith(AndroidJUnit4.class)
public class AndroidTests {
    //We need an activity for these tests
    @Rule
    public ActivityTestRule rule = new ActivityTestRule<>(MainActivity.class);

    @Before
    public void setUp() {
        //NOTE: we need to force Embeddinator to initialize, using Native_managed_dll.INSTANCE does this
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
    public void createActivity() throws Throwable {
        Intent intent = new Intent(rule.getActivity(), ActivitySubclass.class);
        ActivitySubclass a = (ActivitySubclass)rule.launchActivity(intent);
        assertEquals("Hello from C#!", a.getText());
    }
}
