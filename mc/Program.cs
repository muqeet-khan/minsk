﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace mc
{
    class Program
    {
        static void Main(string[] args)
        {
            while(true)
            {
                Console.Write("> ");

                var line = Console.ReadLine();
                if(string.IsNullOrWhiteSpace(line))
                    Environment.Exit(0);
                
                var parser = new Parser(line);
                var expression = parser.Parse();

                PrettyPrint(expression);

                // var lexer = new Lexer(line);
                // while(true)
                // {
                //     var token = lexer.NextToken();
                //     if(token.Kind == SyntaxKind.EndOfFileToken)
                //         break;
                //     Console.WriteLine($"Found token: {token.Kind} at position {token.Position} with value: {token.Value}");
                // }
            }
        }

        static void PrettyPrint(SyntaxNode node,string indent = "")
        {
            Console.Write(indent);
            Console.Write(node.Kind);

            if(node is SyntaxToken t && t.Value != null)
            {
                Console.Write(" ");
                Console.Write(t.Value);
                
            }
            Console.WriteLine();

            indent += "    ";

            foreach (var child in node.GetChildren())
            {
                PrettyPrint(child,indent);
            }
        }
    }

    class Lexer
    {
        private readonly string _text;
        private int _position;

        public Lexer(string text)
        {
            _text = text;
        }

        private char Current
        {
            get
            {
                if(_position >= _text.Length)
                    return '\0';
                
                return _text[_position];
            }
        }

        public SyntaxToken NextToken()
        {
                // number - if you start reading a number then continue reading till its a number 
                // + - * / ( )
                // whitespace

                if(_position >= _text.Length)
                    return new SyntaxToken(SyntaxKind.EndOfFileToken,_position,"\0",null);

                if(char.IsDigit(Current))
                {
                    var start   = _position;
                    
                    while(char.IsDigit(Current))
                        Next();
                    
                    var length = _position - start;
                    var text = _text.Substring(start,length);
                    int.TryParse(text,out var value);
                    return new SyntaxToken(SyntaxKind.NumberToken, start, text,value);
                }

                if(char.IsWhiteSpace(Current))
                {
                    var start   = _position;

                    while(char.IsWhiteSpace(Current))
                        Next();
                    
                    var length = _position - start;
                    var text = _text.Substring(start,length);
                    return new SyntaxToken(SyntaxKind.WhitespaceToken, start, text,null);
                }

                if(Current == '+')
                    return new SyntaxToken(SyntaxKind.PlusToken, _position++,"+",null);
                else if(Current == '-' )
                    return new SyntaxToken(SyntaxKind.MinusToken, _position++,"-",null);
                else if(Current == '*' )
                    return new SyntaxToken(SyntaxKind.StarToken, _position++,"-",null);
                else if(Current == '/' )
                    return new SyntaxToken(SyntaxKind.SlashToken, _position++,"-",null);
                else if(Current == '(' )
                    return new SyntaxToken(SyntaxKind.OpenParenToken, _position++,"-",null);
                else if(Current == ')' )
                    return new SyntaxToken(SyntaxKind.CloseParenToken, _position++,"-",null);
        
                return new SyntaxToken(SyntaxKind.BadToken,_position++,_text.Substring(_position-1,1),null);
        }

        private void Next()
        {
            _position++;
        }
    }

    public enum SyntaxKind
    {
        NumberToken,
        WhitespaceToken,
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        OpenParenToken,
        CloseParenToken,
        BadToken,
        EndOfFileToken,
        NumberExpression,
        BinaryExpression
    }
    public class SyntaxToken : SyntaxNode
    {

        public SyntaxToken(SyntaxKind kind, int position, string text, object value)
        {
            Kind = kind;
            Position = position;
            Text = text;
            Value = value;
        }

        public override SyntaxKind Kind { get; }
        public int Position { get; }
        public string Text { get; }
        public object Value { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            return Enumerable.Empty<SyntaxNode>();
        }
    }

    public abstract class SyntaxNode
    {
        public abstract SyntaxKind Kind {get;}

        public abstract IEnumerable<SyntaxNode> GetChildren();
    }

    abstract class ExpressionSyntax : SyntaxNode
    {
    }

    sealed class NumberExpressionSyntax: ExpressionSyntax 
    {
        public NumberExpressionSyntax(SyntaxToken numberToken)
        {
            NumberToken = numberToken;
        }
        public override SyntaxKind Kind => SyntaxKind.NumberExpression;
        public SyntaxToken NumberToken { get; }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return NumberToken;
        }
    }

    sealed class BinaryExpressionSyntax : ExpressionSyntax
    {
        public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
        {
            Left = left;
            OperatorToken = operatorToken;
            Right = right;
        }
        public ExpressionSyntax Left { get; }
        public SyntaxToken OperatorToken { get; }
        public ExpressionSyntax Right { get; }
        public override SyntaxKind Kind => SyntaxKind.BinaryExpression;
        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return Left;
            yield return OperatorToken;
            yield return Right;
        }
    }

    class Parser
    {
        private readonly SyntaxToken[] _tokens;
        private int _position;
        public Parser(string text)
        {
            var tokens = new List<SyntaxToken>();
            var lexer = new Lexer(text);
            SyntaxToken token;
            do
            {
                token = lexer.NextToken();
                if(token.Kind != SyntaxKind.WhitespaceToken)
                {
                    tokens.Add(token);
                }
            }while(token.Kind != SyntaxKind.EndOfFileToken);

            _tokens = tokens.ToArray();
        }

        private SyntaxToken Peek(int offset)
        {
            var index = _position + offset;
            if(index >= _tokens.Length)
                return _tokens[_tokens.Length -1];

            return _tokens[index];
        }

        private SyntaxToken Current => Peek(0);

        private SyntaxToken NextToken()
        {
            var current = Current;
            _position++;
            return current;
        }

        private SyntaxToken Match(SyntaxKind kind)
        {
            if(Current.Kind == kind)
                return NextToken();
            
            return new SyntaxToken(kind,Current.Position,null,null);
        }
        public ExpressionSyntax Parse()
        {
            var left = ParsePrimaryExpression();

            while(Current.Kind == SyntaxKind.PlusToken || Current.Kind == SyntaxKind.MinusToken 
                            || Current.Kind == SyntaxKind.StarToken || Current.Kind == SyntaxKind.SlashToken)
            {
                var operatorToken = NextToken();
                var right = ParsePrimaryExpression();
                left = new BinaryExpressionSyntax(left,operatorToken, right);
            }

            return left;
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            var numberToken = Match(SyntaxKind.NumberToken);
            return new NumberExpressionSyntax(numberToken);
        }
    }
}
