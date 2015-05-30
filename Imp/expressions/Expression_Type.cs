namespace imperative.expressions
{
    public enum Expression_Type
    {
        // Expressions
        literal,
        property,
        variable,
        function_call,
        property_function_call,
        platform_function,
        instantiate,
        parent_class,
        operation,
        create_dictionary,
        null_value,
        self,
        portal,
        comment,
        profession,
        anonymous_function,
        if_statement,

        // Statements
        statement,
        function_definition,
        flow_control,
        assignment,
        declare_variable,
        scope,
        insert,
        iterator,

        // Summoner
        statements,
        snippet
    }
}