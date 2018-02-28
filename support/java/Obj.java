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

/**
 *
 * @author jsimms
 */
 /*
 XOBJ is the base object that houses the value. XREF and XOUT are classes that
 internally use XOBJ. The classes XOBJ, XREF, and XOUT have methods that allow the object to be
 used as XREF or XOUT parameter; This is important, because objects of these types are 
 interchangeable.

 See Method:
    XXX.Ref()
    XXX.Out()


 The below example shows how to use XOBJ,XREF, and XOUT;  
 //
 // reference parameter example
 //
 void AddToTotal(int a, XREF<Integer> Total)
 {
    Total.Obj.Value += a;
 }
 //
 // out parameter example
 //
 void Add(int a, int b, XOUT<Integer> ParmOut)
 {
    ParmOut.Obj.Value = a+b;
 }
 // 
 // XOBJ example
 //
 int XObjTest()
 {
    XOBJ<Integer> Total = new XOBJ<>(0);    
    Add(1,2,Total.Out());      // example of using out parameter
    AddToTotal(1,Total.Ref()); // example of using ref parameter
    return(Total.Value);
 }
 */
public class Obj<T> {

    public T Value;

    public  Obj() {

    }    
    public Obj(T value) {
        this.Value = value;
    }
    //
    // Method: Ref()
    // Purpose: returns a Reference Parameter object using the XOBJ value
    //    
    public Ref<T> Ref()
    {
        Ref<T> ref = new Ref<T>();
        ref.Obj = this;
        return(ref);
    }
    //
    // Method: Out()
    // Purpose: returns an Out Parameter Object using the XOBJ value
    //
    public Out<T> Out()
    {
        Out<T> out = new Out<T>();
        out.Obj = this;
        return(out);
    }    
    //
    // Method get()
    // Purpose: returns the value 
    // Note: Because this is combersome to edit in the code,
    // the Value object has been made public
    //    
    public T get() {
        return Value;
    }
    //
    // Method get()
    // Purpose: sets the value
    // Note: Because this is combersome to edit in the code,
    // the Value object has been made public
    //
    public void set(T anotherValue) {
        Value = anotherValue;
    }


    @Override
    public String toString() {
        return Value.toString();
    }

    @Override
    public boolean equals(Object obj) {
        return Value.equals(obj);
    }

    @Override
    public int hashCode() {
        return Value.hashCode();
    }
}