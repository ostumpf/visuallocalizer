using System  ;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using EnvDTE;
using VisualLocalizer.Editor;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Text.RegularExpressions;
using VisualLocalizer.Gui;
using EnvDTE80;
using Microsoft.VisualStudio.OLE.Interop;
using VisualLocalizer.Components;
using VisualLocalizer.Library;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.Threading;

namespace VisualLocalizer.Commands {
    internal sealed class MoveToResourcesCommand : AbstractCommand {

        public override void Process() {
            base.Process();

            CodeStringResultItem resultItem = GetReplaceStringItem();

            if (resultItem != null) {
                TextSpan replaceSpan = resultItem.ReplaceSpan;
                string referencedCodeText = resultItem.Value;
                
                SelectResourceFileForm f = new SelectResourceFileForm(
                    referencedCodeText.CreateKeySuggestions(resultItem.NamespaceElement == null ? null : (resultItem.NamespaceElement as CodeNamespace).FullName,
                        resultItem.ClassOrStructElementName, resultItem.MethodElementName == null ? resultItem.VariableElementName : resultItem.MethodElementName),
                    referencedCodeText,
                    currentDocument.ProjectItem.ContainingProject
                );
                System.Windows.Forms.DialogResult result = f.ShowDialog(System.Windows.Forms.Form.FromHandle(new IntPtr(VisualLocalizerPackage.Instance.DTE.MainWindow.HWnd)));

                if (result == System.Windows.Forms.DialogResult.OK) {
                    string referenceText;
                    bool addNamespace;

                    List<CodeUsing> usedNamespaces = resultItem.NamespaceElement.GetUsedNamespaces(resultItem.SourceItem);
                    if (f.UsingFullName) {
                        referenceText = f.SelectedItem.Namespace + "." + f.SelectedItem.Class + "." + f.Key;
                        addNamespace = false;
                    } else {
                        referenceText = f.SelectedItem.Class + "." + f.Key;
                        addNamespace = true;
                        foreach (CodeUsing c in usedNamespaces) {
                            if (c.Namespace == f.SelectedItem.Namespace) {
                                addNamespace = false;
                                if (!string.IsNullOrEmpty(c.Alias)) referenceText = c.Alias + "." + referenceText;                                
                                break;
                            }
                        }
                    }
                    
                    int hr=textLines.ReplaceLines(replaceSpan.iStartLine, replaceSpan.iStartIndex, replaceSpan.iEndLine, replaceSpan.iEndIndex,
                        Marshal.StringToBSTR(referenceText), referenceText.Length, new TextSpan[] { replaceSpan });
                    Marshal.ThrowExceptionForHR(hr);

                    hr = textView.SetSelection(replaceSpan.iStartLine, replaceSpan.iStartIndex,
                        replaceSpan.iStartLine, replaceSpan.iStartIndex + referenceText.Length);
                    Marshal.ThrowExceptionForHR(hr);

                    if (addNamespace)
                        currentDocument.AddUsingBlock(f.SelectedItem.Namespace);                    

                    if (f.Result == SELECT_RESOURCE_FILE_RESULT.INLINE) {
                        // DO NOTHING
                    } else if (f.Result == SELECT_RESOURCE_FILE_RESULT.OVERWRITE) {
                        f.SelectedItem.AddString(f.Key, f.Value);
                        CreateMoveToResourcesOverwriteUndoUnit(f.Key, f.Value, f.OverwrittenValue, f.SelectedItem, addNamespace);
                    } else {
                        f.SelectedItem.AddString(f.Key, f.Value);
                        CreateMoveToResourcesUndoUnit(f.Key, f.Value, f.SelectedItem, addNamespace);
                    }
                }
            } else throw new Exception("This part of code cannot be referenced");
        }      

        private void CreateMoveToResourcesUndoUnit(string key,string value, ResXProjectItem resXProjectItem,bool addNamespace) {            
            List<IOleUndoUnit> units = undoManager.RemoveTopFromUndoStack(addNamespace ? 2 : 1);
            MoveToResourcesUndoUnit newUnit = new MoveToResourcesUndoUnit(key, value, resXProjectItem);
            newUnit.AppendUnits.AddRange(units);
            undoManager.Add(newUnit);
        }

        private void CreateMoveToResourcesOverwriteUndoUnit(string key, string newValue,string oldValue, ResXProjectItem resXProjectItem, bool addNamespace) {
            List<IOleUndoUnit> units = undoManager.RemoveTopFromUndoStack(addNamespace ? 2 : 1);
            MoveToResourcesOverwriteUndoUnit newUnit = new MoveToResourcesOverwriteUndoUnit(key, oldValue, newValue, resXProjectItem);
            newUnit.AppendUnits.AddRange(units);
            undoManager.Add(newUnit);
        } 

        private string TrimAtAndApos(string value) {
            if (value.StartsWith("@")) value = value.Substring(1);
            return value.Substring(1, value.Length - 2);
        }

        private CodeStringResultItem GetReplaceStringItem() {
            TextSpan[] spans = new TextSpan[1];
            int hr = textView.GetSelectionSpan(spans);
            Marshal.ThrowExceptionForHR(hr);

            TextSpan selectionSpan = spans[0];
            object o;
            hr = textLines.CreateTextPoint(selectionSpan.iStartLine, selectionSpan.iStartIndex, out o);
            Marshal.ThrowExceptionForHR(hr);
            TextPoint selectionPoint = (TextPoint)o;

            TextPoint startPoint = null;
            string text = null;
            bool ok = false;
            CodeStringResultItem result = null;
            string codeFunctionName = null;
            string codeVariableName = null;
            CodeElement2 codeClass = null;

            try {
                CodeFunction2 codeFunction = (CodeFunction2)currentCodeModel.CodeElementFromPoint(selectionPoint, vsCMElement.vsCMElementFunction);
                codeFunctionName = codeFunction.Name;
                codeClass = codeFunction.GetClass();

                startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);
                text = codeFunction.GetText();
                ok = true;
            } catch (Exception) {
                try {
                    CodeProperty codeProperty = (CodeProperty)currentCodeModel.CodeElementFromPoint(selectionPoint, vsCMElement.vsCMElementProperty);
                    codeFunctionName = codeProperty.Name;
                    codeClass = codeProperty.GetClass();

                    startPoint = codeProperty.GetStartPoint(vsCMPart.vsCMPartBody);
                    text = codeProperty.GetText();
                    ok = true;
                } catch (Exception) {
                    CodeVariable2 codeVariable = (CodeVariable2)currentCodeModel.CodeElementFromPoint(selectionPoint, vsCMElement.vsCMElementVariable);
                    if (codeVariable.ConstKind != vsCMConstKind.vsCMConstKindConst &&
                        codeVariable.Type.TypeKind == vsCMTypeRef.vsCMTypeRefString &&
                        codeVariable.InitExpression != null) {

                        codeVariableName = codeVariable.Name;
                        text = codeVariable.GetText();
                        startPoint = codeVariable.StartPoint;
                        codeClass = codeVariable.GetClass();
                        if (codeClass.Kind == vsCMElement.vsCMElementClass)
                            ok = true;
                    }
                }
                
            }

            if (ok) {
                CodeStringLookuper lookuper = new CodeStringLookuper(text, startPoint.Line, startPoint.LineCharOffset, startPoint.AbsoluteCharOffset,
                    codeClass.GetNamespace(), codeClass.Name, codeFunctionName, codeVariableName);
                List<CodeStringResultItem> items = lookuper.LookForStrings();

                foreach (CodeStringResultItem item in items) {
                    if (item.ReplaceSpan.Contains(selectionSpan)) {
                        result = item;
                        result.SourceItem = currentDocument.ProjectItem;
                        break;
                    }
                }
            }

            return result;
             
        }

               
    }
}
