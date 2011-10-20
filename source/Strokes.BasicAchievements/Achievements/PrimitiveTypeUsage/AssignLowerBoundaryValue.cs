﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Strokes.BasicAchievements.NRefactory;
using Strokes.Core;
using ICSharpCode.NRefactory.CSharp;
using Strokes.Core.Service.Model;

namespace Strokes.BasicAchievements.Achievements
{
    public abstract class AssignLowerBoundaryValue<T> : NRefactoryAchievement
        where T : struct
    {
        protected override AbstractAchievementVisitor CreateVisitor(StatisAnalysisSession statisAnalysisSession)
        {
            return new Visitor();
        }

        private class Visitor : AbstractAchievementVisitor
        {
            public override object VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, object data)
            {
                var memberReferenceInitializers = variableDeclarationStatement.Variables.Select(a => a.Initializer).OfType<MemberReferenceExpression>();
                if(memberReferenceInitializers.Any(a => IsMemberReferenceOfType<T>(a) && a.MemberName == "MinValue"))
                {
                    UnlockWith(variableDeclarationStatement);
                }
                return base.VisitVariableDeclarationStatement(variableDeclarationStatement, data);
            }

            public override object VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
            {
                var mre = assignmentExpression.Right as MemberReferenceExpression;
                if (mre != null && assignmentExpression.Operator == AssignmentOperatorType.Assign && IsMemberReferenceOfType<T>(mre) && mre.MemberName == "MinValue")
                {
                    UnlockWith(assignmentExpression);
                }

                return base.VisitAssignmentExpression(assignmentExpression, data);
            }

            // TODO: Refactor into using NRefactoryHelpers
            private static bool IsMemberReferenceOfType<TMemberType>(MemberReferenceExpression expression)
            {
                var variations = new List<string>();
                if(typeof(TMemberType) == typeof(System.Int32))
                {
                    variations = new List<string>() {"System.Int32", "Int32", "int"};
                }
                else if(typeof(TMemberType) == typeof(System.Double))
                {
                    variations = new List<string>() { "System.Double", "Double", "double" };
                }
                else if (typeof(TMemberType) == typeof(System.Single))
                {
                    variations = new List<string>() { "System.Single", "Single", "float" };
                }
                else if (typeof(TMemberType) == typeof(System.Char))
                {
                    variations = new List<string>() { "System.Char", "Char", "char" };
                }

                return variations.Any(a => a == expression.Target.ToString());
            }
        }
    }
}
