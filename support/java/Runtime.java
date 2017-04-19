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

public final class Runtime {
    interface RuntimeLibrary extends com.sun.jna.Library {
        public void mono_embeddinator_set_assembly_path(String path);
    }

    public static RuntimeLibrary runtimeLibrary;

    public static boolean initialized;

    public static void initialize(String library) {
        runtimeLibrary = Native.loadLibrary(library, RuntimeLibrary.class);

        String assemblyPath = System.getProperty("user.dir") + "/" + library;
        runtimeLibrary.mono_embeddinator_set_assembly_path(assemblyPath);
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
