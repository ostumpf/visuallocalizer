using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Commands;
using EnvDTE80;
using EnvDTE;
using VisualLocalizer.Library;

namespace VisualLocalizer.Components {
    internal sealed class VBCodeExplorer : CSharpCodeExplorer {
        private VBCodeExplorer() { }

        private static VBCodeExplorer instance;
        public static new VBCodeExplorer Instance {
            get {
                if (instance == null) instance = new VBCodeExplorer();
                return instance;
            }
        }

        protected override void Explore(AbstractBatchCommand parentCommand, CodeFunction2 codeFunction, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {
            if (codeFunction.MustImplement) return;
            if (!exploreable(codeFunction as CodeElement)) return;

            string functionText = codeFunction.GetText();
            if (string.IsNullOrEmpty(functionText)) return;

            TextPoint startPoint = codeFunction.GetStartPoint(vsCMPart.vsCMPartBody);
            bool functionLocalizableFalse = HasLocalizableFalseAttribute(codeFunction as CodeElement);

            var list = parentCommand.LookupInVB(functionText, startPoint, parentNamespace, codeClassOrStruct, codeFunction.Name, null, isLocalizableFalse || functionLocalizableFalse);
            EditPoint2 editPoint = (EditPoint2)startPoint.CreateEditPoint();
            foreach (AbstractResultItem item in list)
                AddContextToItem(item, editPoint);
        }

        protected override void Explore(AbstractBatchCommand parentCommand, CodeVariable2 codeVariable, CodeNamespace parentNamespace, CodeElement2 codeClassOrStruct, Predicate<CodeElement> exploreable, bool isLocalizableFalse) {
            if (codeVariable.ConstKind == vsCMConstKind.vsCMConstKindConst) return;
            if (codeVariable.Type.TypeKind != vsCMTypeRef.vsCMTypeRefString) return;
            if (codeVariable.InitExpression == null) return;
            if (codeClassOrStruct.Kind == vsCMElement.vsCMElementStruct && !codeVariable.IsShared) return;
            if (!exploreable(codeVariable as CodeElement)) return;

            string initExpression = codeVariable.GetText();
            if (string.IsNullOrEmpty(initExpression)) return;

            TextPoint startPoint = codeVariable.StartPoint;
            bool variableLocalizableFalse = HasLocalizableFalseAttribute(codeVariable as CodeElement);

            var list = parentCommand.LookupInVB(initExpression, startPoint, parentNamespace, codeClassOrStruct, null, codeVariable.Name, isLocalizableFalse || variableLocalizableFalse);

            EditPoint2 editPoint = (EditPoint2)startPoint.CreateEditPoint();
            foreach (AbstractResultItem item in list)
                AddContextToItem(item, editPoint);
        }
    }
}
