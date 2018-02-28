package mono.embeddinator;

import com.sun.jna.*;
import java.io.*;
import mono.embeddinator.Runtime.RuntimeLibrary;

public class DesktopImpl {
    public RuntimeLibrary initialize(String library) {
        System.setProperty("jna.encoding", "utf8");

        String tmp = System.getProperty("java.io.tmpdir");
        String assemblyPath = extractAssembly(tmp, library);

        RuntimeLibrary runtimeLibrary = Native.loadLibrary(library, RuntimeLibrary.class);
        runtimeLibrary.mono_embeddinator_set_assembly_path(assemblyPath);
        runtimeLibrary.mono_embeddinator_set_runtime_assembly_path(assemblyPath);

        //NOTE: need to make sure mscorlib.dll is extracted & directory set
        String monoPath = Utilities.combinePath(tmp, "mono", "4.5");
        File monoFile = new File(monoPath);
        if (!monoFile.isDirectory()) {
            monoFile.mkdirs();
            monoFile.deleteOnExit();
        }

        extractAssembly(monoPath, "mscorlib");

        return runtimeLibrary;
    }

    public String getResourcePath(String library) {
        return "/assemblies/" + library + ".dll";
    }

    public String extractAssembly(String tmp, String library) {
        String assemblyPath = Utilities.combinePath(tmp, library);
        File assemblyFile = new File(assemblyPath + ".dll");

        String resourcePath = getResourcePath(library);
        InputStream input = Runtime.class.getResourceAsStream(resourcePath);
        if (input == null) {
            throw new RuntimeException("Unable to locate " + resourcePath + " within jar file!");
        }

        try {
            OutputStream output = new FileOutputStream(assemblyFile);
            try {
                byte[] buffer = new byte[4 * 1024];
                int read;
                while ((read = input.read(buffer)) != -1) {
                    output.write(buffer, 0, read);
                }
                output.flush();
            } finally {
                output.close();
                input.close();
            }

            //NOTE: this file should be temporary
            assemblyFile.deleteOnExit();
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
        return assemblyPath;
    }
}
