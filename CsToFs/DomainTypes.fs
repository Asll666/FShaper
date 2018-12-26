﻿namespace CsToFs
open Microsoft.FSharp.Compiler.Ast

[<NoEquality; NoComparison;RequireQualifiedAccess>]
type Pat =
    | Const of SynConst
    | Wild
    | Named of Pat * Ident *  isSelfIdentifier:bool (* true if 'this' variable *)  * accessibility:SynAccess option
    | Typed of SynPat * SynType
    | Attrib of SynPat * SynAttributes
    | Or of SynPat * SynPat
    | Ands of SynPat list
    | LongIdent of longDotId:LongIdentWithDots * (* holds additional ident for tooling *) Ident option * SynValTyparDecls option (* usually None: temporary used to parse "f<'a> x = x"*) * SynConstructorArgs  * accessibility:SynAccess option
    | Tuple of SynPat list
    | StructTuple of SynPat list
    | Paren of SynPat
    | ArrayOrList of bool * SynPat list
    | Record of ((LongIdent * Ident) * SynPat) list
    /// 'null'
    | Null
    /// '?id' -- for optional argument names
    | OptionalVal of Ident
    /// ':? type '
    | IsInst of SynType
    /// &lt;@ expr @&gt;, used for active pattern arguments
    | QuoteExpr of Expr

    /// Deprecated character range:ranges
    | DeprecatedCharRange of char * char
    /// Used internally in the type checker
    | InstanceMember of  Ident * Ident * (* holds additional ident for tooling *) Ident option * accessibility:SynAccess option (* adhoc overloaded method/property *)

    /// A pattern arising from a parse error
    | FromParseError of SynPat
and 
    [<NoEquality;NoComparison>]
    CsToFsBinding  = 
    | LetBind of 
        accessibility:SynAccess option *
        kind:SynBindingKind *
        mustInline:bool *
        isMutable:bool *
        attrs:SynAttributes *
        valData:SynValData *
        headPat:Pat *
        expr:Expr

and
    [<NoEquality; NoComparison;RequireQualifiedAccess>]
     Expr  = 

    /// F# syntax: (expr)
    ///
    /// Paren(expr, leftParenRange, rightParenRange, wholeRangeIncludingParentheses)
    ///
    /// Parenthesized expressions. Kept in AST to distinguish A.M((x,y))
    /// from A.M(x,y), among other things.
    | Paren of expr:Expr

    /// F# syntax: 1, 1.3, () etc.
    | Const of constant:SynConst

    /// F# syntax: expr : type
    | Typed of  expr:Expr * synType:SynType 

    /// F# syntax: e1, ..., eN
    | Tuple of exprs:Expr list

    ///// F# syntax: struct (e1, ..., eN)
    //| StructTuple of  exprs:Expr list

    ///// F# syntax: [ e1; ...; en ], [| e1; ...; en |]
    //| ArrayOrList of  isList:bool * exprs:Expr list

    /// F# syntax: new C(...)
    /// The flag is true if known to be 'family' ('protected') scope
    | New of isProtected:bool * typeName:SynType
    
    /// F# syntax: 'while ... do ...'
    | While of whileSeqPoint:SequencePointInfoForWhileLoop * whileExpr:Expr * doExpr:Expr

    /// F# syntax: 'for i = ... to ... do ...'
    | For of forSeqPoint:SequencePointInfoForForLoop * ident:Ident * identBody:Expr * bool * toBody:Expr * doBody:Expr

    /// Expr.ForEach (spBind, seqExprOnly, isFromSource, pat, enumExpr, bodyExpr, mWholeExpr).
    ///
    /// F# syntax: 'for ... in ... do ...'
    | ForEach of forSeqPoint:SequencePointInfoForForLoop * seqExprOnly:SeqExprOnly * isFromSource:bool * pat:SynPat * enumExpr:Expr * bodyExpr:Expr

    /// F# syntax: [ expr ], [| expr |]
    | ArrayOrListOfSeqExpr of isArray:bool * expr:Expr

    /// CompExpr(isArrayOrList, isNotNakedRefCell, expr)
    ///
    /// F# syntax: { expr }
    | CompExpr of isArrayOrList:bool * isNotNakedRefCell:bool ref * expr:Expr

    /// First bool indicates if lambda originates from a method. Patterns here are always "simple"
    /// Second bool indicates if this is a "later" part of an iterated sequence of lambdas
    ///
    /// F# syntax: fun pat -> expr
    | Lambda of  fromMethod:bool * inLambdaSeq:bool * args:SynSimplePats * body:Expr

    ///// F# syntax: function pat1 -> expr | ... | patN -> exprN
    //| MatchLambda of isExnMatch:bool * range * SynMatchClause list * matchSeqPoint:SequencePointInfoForBinding

    ///// F# syntax: match expr with pat1 -> expr | ... | patN -> exprN
    //| Match of  matchSeqPoint:SequencePointInfoForBinding * expr:Expr * clauses:SynMatchClause list * isExnMatch:bool (* bool indicates if this is an exception match in a computation expression which throws unmatched exceptions *)

    ///// F# syntax: do expr
    //| Do of  expr:Expr

    /// F# syntax: assert expr
    | Assert of expr:Expr

    /// App(exprAtomicFlag, isInfix, funcExpr, argExpr, m)
    ///  - exprAtomicFlag: indicates if the application is syntactically atomic, e.g. f.[1] is atomic, but 'f x' is not
    ///  - isInfix is true for the first app of an infix operator, e.g. 1+2 becomes App(App(+,1),2), where the inner node is marked isInfix
    ///      (or more generally, for higher operator fixities, if App(x,y) is such that y comes before x in the source code, then the node is marked isInfix=true)
    ///
    /// F# syntax: f x
    | App of ExprAtomicFlag * isInfix:bool * funcExpr:Expr * argExpr:Expr

    /// TypeApp(expr, mLessThan, types, mCommas, mGreaterThan, mTypeArgs, mWholeExpr)
    ///     "mCommas" are the ranges for interstitial commas, these only matter for parsing/design-time tooling, the typechecker may munge/discard them
    ///
    /// F# syntax: expr<type1,...,typeN>
    //| TypeApp of expr:Expr * LESSrange:range * typeNames:SynType list * commaRanges:range list * GREATERrange:range option * typeArgsRange:range

    /// LetOrUse(isRecursive, isUse, bindings, body, wholeRange)
    ///
    /// F# syntax: let pat = expr in expr
    /// F# syntax: let f pat1 .. patN = expr in expr
    /// F# syntax: let rec f pat1 .. patN = expr in expr
    /// F# syntax: use pat = expr in expr
    | LetOrUse of isRecursive:bool * isUse:bool * bindings:CsToFsBinding list * body:Expr

    /// F# syntax: try expr with pat -> expr
    | TryWith of tryExpr:Expr * withCases:SynMatchClause list * trySeqPoint:SequencePointInfoForTry * withSeqPoint:SequencePointInfoForWith

    /// F# syntax: try expr finally expr
    | TryFinally of tryExpr:Expr * finallyExpr:Expr * trySeqPoint:SequencePointInfoForTry * finallySeqPoint:SequencePointInfoForFinally

    /// F# syntax: lazy expr
    | Lazy of Expr

    /// Seq(seqPoint, isTrueSeq, e1, e2, m)
    ///  isTrueSeq: false indicates "let v = a in b; v"
    ///
    /// F# syntax: expr; expr
    | Sequential of seqPoint:SequencePointInfoForSeq * isTrueSeq:bool * expr1:Expr * expr2:Expr

    ///  IfThenElse(exprGuard,exprThen,optionalExprElse,spIfToThen,isFromErrorRecovery,mIfToThen,mIfToEndOfLastBranch)
    ///
    /// F# syntax: if expr then expr
    /// F# syntax: if expr then expr else expr
    | IfThenElse of ifExpr:Expr * thenExpr:Expr * elseExpr:Expr option * spIfToThen:SequencePointInfoForBinding * isFromErrorRecovery:bool
    /// F# syntax: ident
    /// Optimized representation, = Expr.LongIdent(false,[id],id.idRange)
    | Ident of string

    /// F# syntax: ident.ident...ident
    /// LongIdent(isOptional, longIdent, altNameRefCell, m)
    ///   isOptional: true if preceded by a '?' for an optional named parameter
    ///   altNameRefCell: Normally 'None' except for some compiler-generated variables in desugaring pattern matching. See SynSimplePat.Id
    | LongIdent of isOptional:bool * longDotId:LongIdentWithDots

    /// F# syntax: ident.ident...ident <- expr
    | LongIdentSet of longDotId:LongIdentWithDots * expr:Expr
    /// DotGet(expr, rangeOfDot, lid, wholeRange)
    ///
    /// F# syntax: expr.ident.ident
    | DotGet of expr:Expr

    /// F# syntax: expr.ident...ident <- expr
    | DotSet of Expr * longDotId:LongIdentWithDots * Expr

    /// F# syntax: expr <- expr
    | Set of Expr * Expr

    /// F# syntax: expr.[expr,...,expr]
    | DotIndexedGet of Expr * SynIndexerArg list

    /// DotIndexedSet (objectExpr, indexExprs, valueExpr, rangeOfLeftOfSet, rangeOfDot, rangeOfWholeExpr)
    ///
    /// F# syntax: expr.[expr,...,expr] <- expr
    | DotIndexedSet of objectExpr:Expr * indexExprs:SynIndexerArg list * valueExpr:Expr

    /// F# syntax: Type.Items(e1) <- e2 , rarely used named-property-setter notation, e.g. Foo.Bar.Chars(3) <- 'a'
    | NamedIndexedPropertySet of longDotId:LongIdentWithDots * Expr * Expr

    /// F# syntax: expr.Items(e1) <- e2 , rarely used named-property-setter notation, e.g. (stringExpr).Chars(3) <- 'a'
    //| DotNamedIndexedPropertySet of Expr * longDotId:LongIdentWithDots * Expr * Expr

    /// F# syntax: expr :? type
    | TypeTest of  expr:Expr * typeName:SynType

    /// F# syntax: expr :> type
    | Upcast of  expr:Expr * typeName:SynType 

    /// F# syntax: expr :?> type
    | Downcast of  expr:Expr * typeName:SynType 
    /// F# syntax: upcast expr
    | InferredUpcast of  expr:Expr 

    /// F# syntax: downcast expr
    | InferredDowncast of  expr:Expr 

    /// F# syntax: null
    | Null 

    /// F# syntax: &expr, &&expr
    | AddressOf of  isByref:bool * Expr 

    /// F# syntax: ((typar1 or ... or typarN): (member-dig) expr)
    | TraitCall of SynTypar list * SynMemberSig * Expr 

    /// F# syntax: ... in ...
    /// Computation expressions only, based on JOIN_IN token from lex filter
    //| JoinIn of Expr * Expr 

    /// F# syntax: <implicit>
    /// Computation expressions only, implied by final "do" or "do!"
    //| ImplicitZero of range:range

    /// F# syntax: yield expr
    /// F# syntax: return expr
    /// Computation expressions only
    //| YieldOrReturn   of (bool * bool) * expr:Expr

    /// F# syntax: yield! expr
    /// F# syntax: return! expr
    /// Computation expressions only
    //| YieldOrReturnFrom  of (bool * bool) * expr:Expr

    /// Expr.LetOrUseBang(spBind, isUse, isFromSource, pat, rhsExpr, bodyExpr, mWholeExpr).
    ///
    /// F# syntax: let! pat = expr in expr
    /// F# syntax: use! pat = expr in expr
    /// Computation expressions only
    | LetOrUseBang    of bindSeqPoint:SequencePointInfoForBinding * isUse:bool * isFromSource:bool * SynPat * Expr * Expr

    /// F# syntax: match! expr with pat1 -> expr | ... | patN -> exprN
    //| MatchBang of  matchSeqPoint:SequencePointInfoForBinding * expr:Expr * clauses:SynMatchClause list * isExnMatch:bool (* bool indicates if this is an exception match in a computation expression which throws unmatched exceptions *)

    /// F# syntax: do! expr
    /// Computation expressions only
    | DoBang      of expr:Expr
    /// Only used in FSharp.Core
    //| LibraryOnlyILAssembly of ILInstr array *  SynType list * Expr list * SynType list (* Embedded IL assembly code *)

    /// 'use x = fixed expr'
    | Fixed of expr:Expr

    | InLetPlaceholder
    

//type Line = 
    //| Line of string 
    //| Expr of Expr

//module Line = 

    //let append (Line l) (s:string)  = 
    //    l + s |> Line

    //let prepend (s:string) (Line l) = 
    //    s + l |> Line

    //let join sep (Line l) (s:string) =
    //    sprintf "%s%s%s" l sep s |> Line

    //let concat sep lines = 
        //lines 
        //|> Seq.map (fun (Line l) -> l)
        //|> String.concat sep
        //|> Line

    | ReturnFromIf



type Line = 
    | Line of string

module Line = 
    let concat a b = Line ""
    let append a b = Line "" 
    let prepend a b = Line "" 


type Parameter = {
    Type:string
    Name:string
}

type Ctor = {
    Body:Line
    Parameters: Parameter list
    SubclassArgs: string list
}

type Method = {
    Name:string
    Parameters:Parameter list
    Body: Expr
    ReturnType:string
    IsVirtual:bool
    IsAsync:bool
    IsPrivate: bool
    IsOverride:bool
    Accessibility:SynAccess option
}

type Prop = {
    Name:string
    Type:string
    Get: Expr option
    Set: Expr option
    Access: SynAccess option
}

type ClassName = {
    Name:string
    Generics:string list
}

type Field = {
    IsPublic: bool // this syntax is not supported in F# 
    Name:string
    Type: string
    Initilizer:Expr option
}

type Attribute = {
    Name:string
    Parameters:string option
}

type Class = {
    Name:ClassName
    Constructors:Ctor list
    Fields: Field list
    Methods: Method list
    Properties: Prop list
    Attributes: Attribute list
    BaseClass: string option
    ImplementInterfaces: string list
    TypeParameters:string list
}

type Interface = {
    Name:string
    // TDOO Generics
    Methods:string list
}

type UsingStatement = {
    Namespace:string
}

type Namespace = {
    Name:string
    Interfaces: Interface list
    Classes: Class list
}

type File = {
    UsingStatements:UsingStatement list
    Namespaces:Namespace list
}

type FsharpSyntax = 
    | File of File
    | UsingStatement of UsingStatement
    | Namespace of Namespace
    | Interface of Interface
    | Class of Class
    | Field of Field seq
    | Prop of Prop
    | Method of Method
    | Empty
