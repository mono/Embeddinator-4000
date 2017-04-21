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
 *
 * Original code from http://stackoverflow.com/a/22537292
 */

package mono.embeddinator;

public class Out<T> {

    public Obj<T> Obj = null;

    public Out(T value) {
        Obj = new Obj<T>(value);
    }

    public Out() {
        Obj = new Obj<T>();
    }  

    public Out<T> Out() {
        return(this);
    }

    public Ref<T> Ref() {
        return(Obj.Ref());
    }

    public T get() {
        return Obj.get();
    }

    public void set(T value) {
        Obj.set(value);
    }
};
