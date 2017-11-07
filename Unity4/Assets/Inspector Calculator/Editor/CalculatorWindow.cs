using System;
using UnityEngine;
using UnityEditor;
using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using System.Reflection;

public class CalculatorWindow : EditorWindow 
{ 
    [MenuItem ("Edit/Calculate... %#e")]
    static void Evaluate()
    {
        StaticInit();
        
        EditorWindow focusedWindow = EditorWindow.focusedWindow;
        
        if( focusedWindow != null )
        {
            //preserve this so we don't make anybody cry
            string oldCopyBuffer = EditorGUIUtility.systemCopyBuffer;
            
            focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("SelectAll"));
            focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Copy"));
            var oldKeyboardControlID = GUIUtility.keyboardControl;

            CalculatorWindow calcWindow = CalculatorWindow.GetWindow( typeof( CalculatorWindow ) ) as CalculatorWindow;
            calcWindow.Init( focusedWindow, oldKeyboardControlID, oldCopyBuffer );
        }

    }
    
    void Init( EditorWindow inOldFocusedWindow, int inOldKeyboardControlID, string inOldCopyBuffer )
    {
        _OldFocusedWindow = inOldFocusedWindow;
        _ToEval = EditorGUIUtility.systemCopyBuffer;
        _OldKeyboardControlID = inOldKeyboardControlID;
        _OldCopyBuffer = inOldCopyBuffer;
        
    }
    
    
    void OnGUI () 
    {
        
        Event e = Event.current;
        KeyCode keyCode = e.keyCode;
        EventType eventType = e.type;
        
        GUI.SetNextControlName("EvalField");
        
        _ToEval = EditorGUILayout.TextField( "Evaluate: ", _ToEval);
        
        if( GUI.GetNameOfFocusedControl() == "" ) 
        {
            GUI.FocusControl( "EvalField" );
        }
        

        if( keyCode == KeyCode.Return && eventType == EventType.KeyDown )
        {
            //var evaluator : Evaluator = new Evaluator();
            string val  = UserFriendlifyAndEvaluate( _ToEval ); 
            if( string.IsNullOrEmpty( val ) )
            {
                val = _ToEval;
            }
            EditorGUIUtility.systemCopyBuffer = val.ToString();
            Close();
            _OldFocusedWindow.Focus();
            GUIUtility.keyboardControl = _OldKeyboardControlID;
            _OldFocusedWindow.SendEvent(EditorGUIUtility.CommandEvent("SelectAll"));
            _OldFocusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Paste"));
            //_OldFocusedWindow.SendEvent(Event.KeyboardEvent( "return" ) );
            //_OldFocusedWindow.Repaint();
            //GUIUtility.keyboardControl = _OldKeyboardControlID;
            
            //restore!
            EditorGUIUtility.systemCopyBuffer = _OldCopyBuffer;
            
            
        }
        
        
        
    }
    
    

    public string UserFriendlifyAndEvaluate( string inToEval )
    {
        inToEval = inToEval.ToLower();

        CompilerParameters cp = new CompilerParameters();
        cp.ReferencedAssemblies.Add( "System.dll" );
        //if we want to do more, we have to add UnityEngine and then find our assemblies in the library
        //kinda overkill for now...
        cp.GenerateExecutable = false;
        cp.GenerateInMemory = true;
        
        //do weh ave to wrap all the functions we want to work? that's kinda annoying
        string code = "using System;" +
            "namespace Evaluator" +
            "{ " +
            "   public class CreatedEvaluator" +
            "   { " +
            "       public string GetResult()" +
            "       { " +
            "           return (" + inToEval + ").ToString( \"f4\" ); " +
            "       } " +
            "       public double sin( double inD ) { return Math.Sin( inD ); } " +
            "       public double cos( double inD ) { return Math.Cos( inD ); } " +
            "       public double tan( double inD ) { return Math.Tan( inD ); } " +
            "       public double asin( double inD ) { return Math.Asin( inD ); }   " +
            "       public double acos( double inD ) { return Math.Acos( inD ); }   " +
            "       public double atan( double inD ) { return Math.Atan( inD ); }   " +
            "       public double pow( double inA, double inB ) { return Math.Pow( inA, inB ); }    " +
            "       public double exp( double inD ) { return Math.Exp( inD ); } " +
            "       public double ln( double inD ) { return Math.Log( inD ); }  " +
            "       public double log( double inD ) { return Math.Log10( inD ); }   " +
            "       public double pi { get{ return Math.PI; } } " +
            "   } " +
            "}";
        
        // for reference, one of the most inefficient ways to evaluate math. but pretty cool!
        CompilerResults cr = _CSharpProvider.CompileAssemblyFromSource(cp, code );
        
        if (cr.Errors.HasErrors)
        {
            StringBuilder errors = new StringBuilder();
            errors.Append("Error Calculating: ");
            foreach( CompilerError ce in cr.Errors )
            {
                errors.AppendLine( ce.ErrorText );
            }
            Debug.LogError( errors.ToString() );
            return null;
        }
        else
        {       
            Assembly a = cr.CompiledAssembly;
            object createdEvaluator = a.CreateInstance( "Evaluator.CreatedEvaluator" );
            MethodInfo mi = createdEvaluator.GetType().GetMethod( "GetResult" );
            return mi.Invoke( createdEvaluator, null ).ToString();
        }
    }
  
    
    private static void StaticInit()
    {
        if( !_IsInitted )
        {
            _IsInitted = true;
            
            //on the mac we have to add mono into the path so it can be found
            if( Application.platform == RuntimePlatform.OSXEditor )
            {
                string path = Environment.GetEnvironmentVariable( "PATH" );
                path = path + ":" + EditorApplication.applicationPath + "/Contents/Frameworks/Mono/bin";
                Environment.SetEnvironmentVariable( "PATH", path ); 
                path = Environment.GetEnvironmentVariable( "PATH" );
            }
            
            _CSharpProvider = new CSharpCodeProvider();
        }
    }
    
    string          _ToEval;
    EditorWindow    _OldFocusedWindow;
    int             _OldKeyboardControlID;
    string          _OldCopyBuffer;
    
    private static bool _IsInitted = false;
    private static CSharpCodeProvider _CSharpProvider;
    
}
