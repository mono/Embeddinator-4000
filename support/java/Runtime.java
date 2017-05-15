/*
 * Mono Embeddinator-4000 Java support code.
 *
 * Author:
 *   Joao Matos (joao.matos@xamarin.com)
 *
 * (C) 2016 Microsoft, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

package mono.embeddinator;

import com.sun.jna.*;
import com.sun.jna.ptr.*;
import java.util.*;

public final class Runtime {
    interface RuntimeLibrary extends com.sun.jna.Library {
        static interface ErrorType {
            public static final int MONO_EMBEDDINATOR_OK = 0;
            /** Mono managed exception */
            public static final int MONO_EMBEDDINATOR_EXCEPTION_THROWN = 1;
            /** Mono failed to load assembly */
            public static final int MONO_EMBEDDINATOR_ASSEMBLY_OPEN_FAILED = 2;
            /** Mono failed to lookup class */
            public static final int MONO_EMBEDDINATOR_CLASS_LOOKUP_FAILED = 3;
            /** Mono failed to lookup method */
            public static final int MONO_EMBEDDINATOR_METHOD_LOOKUP_FAILED = 4;
        }

        public class Error extends Structure {
            public static class ByValue extends Error implements Structure.ByValue { }

            public int type;
            public Pointer monoException;
            public String string;

            @Override
            protected List getFieldOrder() {
                return Arrays.asList("type", "monoException", "string");
            }
        }

        public class GString extends Structure {
            public static class ByValue extends GString implements Structure.ByValue { }
            public static class ByReference  extends GString implements Structure.ByReference { }

            public Pointer str = Pointer.NULL;
            public NativeLong len = new NativeLong();
            public NativeLong allocated_len = new NativeLong();

            @Override
            protected List getFieldOrder() {
                return Arrays.asList("str", "len", "allocated_len");
            }
        }

        public static interface ErrorCallback extends Callback {
            void invoke(RuntimeLibrary.Error.ByValue error) throws RuntimeException;
        }

        public void mono_embeddinator_set_assembly_path(String path);
        public Pointer mono_embeddinator_install_error_report_hook(ErrorCallback cb);
    }

    public static boolean initialized;

    public static RuntimeLibrary runtimeLibrary;

    public static RuntimeLibrary.ErrorCallback error;

    public static ThreadLocal<RuntimeException> pendingException;

    static {
        pendingException = new ThreadLocal<RuntimeException>();
    }

    public static void initialize(String library) {
        System.setProperty("jna.encoding", "utf8");

        runtimeLibrary = Native.loadLibrary(library, RuntimeLibrary.class);
        
        String assemblyPath = Utilities.combinePath(System.getProperty("user.dir"), library);
        runtimeLibrary.mono_embeddinator_set_assembly_path(assemblyPath);
        error = new RuntimeLibrary.ErrorCallback() {
            public void invoke(RuntimeLibrary.Error.ByValue error) {
                if (error.type == RuntimeLibrary.ErrorType.MONO_EMBEDDINATOR_OK)
                    return;

                pendingException.set(new RuntimeException());
            }
        };

        runtimeLibrary.mono_embeddinator_install_error_report_hook(error);
    }

    public static void checkExceptions() throws RuntimeException {
        RuntimeException exception = pendingException.get();
        pendingException.remove();

        if (exception != null)
            throw exception;
    }

    public static synchronized <T> T loadLibrary(String library, Class<T> klass) {
        if (!initialized) {
            initialize(library);
            initialized = true;
        }

        library = com.sun.jna.Platform.isWindows()
            ? String.format("%s.dll", library) : String.format("lib%s.dylib", library);
        return Native.loadLibrary(library, klass);
    }
}
